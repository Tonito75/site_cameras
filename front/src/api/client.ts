import type { CameraConfig, CameraImage, PingResult } from '../types/camera';

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

  getCameraHistory: (name: string, useAI: boolean) =>
    request<CameraImage[]>(
      `/api/cameras/${encodeURIComponent(name)}/history?useAI=${useAI}`
    ),
};
