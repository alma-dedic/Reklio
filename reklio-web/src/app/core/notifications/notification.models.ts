export interface AppNotification {
  id: number;
  message: string;
  isRead: boolean;
  claimId: number;
  claimReference: string;
  createdAt: string;
}
