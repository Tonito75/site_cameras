import type { CameraConfig, CameraImage, DayStat, FavouritesPage, FavouritesPageCamera, PingResult } from '../types/camera';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  });

  if (res.status === 401) {
    window.location.href = '/login';
    throw new Error('Unauthorized');
  }

  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}

export const api = {
  login: (password: string) =>
    fetch(`${BASE_URL}/api/login`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: `password=${encodeURIComponent(password)}`,
    }),

  logout: () =>
    fetch(`${BASE_URL}/api/logout`, { method: 'POST', credentials: 'include' }),

  me: () => request<{ isAuthenticated: boolean }>('/api/me'),

  getCameras: () => request<CameraConfig[]>('/api/cameras'),

  pingCamera: (name: string) =>
    request<PingResult>(`/api/cameras/${encodeURIComponent(name)}/ping`),

  getCameraHistory: (name: string, useAI: boolean, skip: number = 0, count: number = 20, fromDate?: string) => {
    const params = new URLSearchParams({ useAI: String(useAI), skip: String(skip), count: String(count) })
    if (fromDate) params.set('fromDate', fromDate)
    return request<CameraImage[]>(`/api/cameras/${encodeURIComponent(name)}/history?${params}`)
  },

  getCameraFavourites: (name: string, skip: number = 0, take: number = 20) =>
    request<FavouritesPageCamera>(`/api/cameras/${encodeURIComponent(name)}/favourites?skip=${skip}&take=${take}`),

  addFavourite: (cameraName: string, imageUrl: string) =>
    request<{ id: number }>('/api/favourites', {
      method: 'POST',
      body: JSON.stringify({ cameraName, imageUrl }),
    }),

  deleteFavourite: (id: number) =>
    request<{ success: boolean }>(`/api/favourites/${id}`, { method: 'DELETE' }),

  getAllFavourites: (skip: number, take: number) =>
    request<FavouritesPage>(`/api/favourites?skip=${skip}&take=${take}`),

  getCameraStats: (name: string) =>
    request<DayStat[]>(`/api/cameras/${encodeURIComponent(name)}/stats`),
};
