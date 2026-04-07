export interface CameraConfig {
  name: string;
  ip: string;
  url: string;
  group: string;
}

export interface CameraImage {
  url: string;
  date: string;
  timeAgo: string;
}

export interface PingResult {
  name: string;
  isOnline: boolean;
}

export interface Favourite {
  id: number;
  url: string;
  addedAt: string;
}

export interface FavouriteWithCamera extends Favourite {
  cameraName: string;
}

export interface FavouritesPage {
  total: number;
  items: FavouriteWithCamera[];
}

export interface FavouritesPageCamera {
  total: number;
  items: Favourite[];
}

export interface DayStat {
  date: string;
  count: number;
}
