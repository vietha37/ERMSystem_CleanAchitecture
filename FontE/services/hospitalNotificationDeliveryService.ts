import api from "./api";
import {
  NotificationDeliveryListResult,
  NotificationDeliverySummary,
  NotificationDeliveryStatus,
} from "./types";

export const hospitalNotificationDeliveryService = {
  getAll: async (
    status: NotificationDeliveryStatus | "All" = "All",
    pageNumber = 1,
    pageSize = 20
  ): Promise<NotificationDeliveryListResult> => {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });

    if (status !== "All") {
      params.set("status", status);
    }

    const response = await api.get<NotificationDeliveryListResult>(
      `/hospital-notification-deliveries?${params.toString()}`
    );

    return response.data;
  },

  getSummary: async (): Promise<NotificationDeliverySummary> => {
    const response = await api.get<NotificationDeliverySummary>(
      "/hospital-notification-deliveries/summary"
    );

    return response.data;
  },

  retry: async (deliveryId: string): Promise<void> => {
    await api.post(`/hospital-notification-deliveries/${deliveryId}/retry`);
  },
};
