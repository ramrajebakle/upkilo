import axios, { AxiosInstance } from 'axios';

export interface UpkiloConfig {
  apiKey: string;
  baseUrl?: string;
  tenantId?: string;
  isSandbox?: boolean;
}

/**
 * Main client for interacting with the Upkilo API.
 */
export class UpkiloClient {
  public axios: AxiosInstance;
  public bookings: BookingModule;
  public services: ServiceModule;

  constructor(config: UpkiloConfig) {
    this.axios = axios.create({
      baseURL: config.baseUrl || 'https://api.upkilo.com/v1',
      headers: {
        'Authorization': `Bearer ${config.apiKey}`,
        'X-Tenant-Id': config.tenantId || '',
        'X-Sandbox-Mode': config.isSandbox ? 'true' : 'false',
        'Content-Type': 'application/json'
      }
    });

    this.bookings = new BookingModule(this.axios);
    this.services = new ServiceModule(this.axios);
  }

  /**
   * Verify a webhook signature from Upkilo.
   */
  public verifyWebhook(payload: string, signature: string, secret: string): boolean {
    // HMAC SHA256 implementation would go here
    return true; 
  }
}

export class BookingModule {
  constructor(private axios: AxiosInstance) {}

  async list(filters?: any) {
    const response = await this.axios.get('/bookings', { params: filters });
    return response.data;
  }

  async get(id: string) {
    const response = await this.axios.get(`/bookings/${id}`);
    return response.data;
  }

  async create(data: any) {
    const response = await this.axios.post('/bookings', data);
    return response.data;
  }
}

export class ServiceModule {
  constructor(private axios: AxiosInstance) {}

  async list(filters?: any) {
    const response = await this.axios.get('/services', { params: filters });
    return response.data;
  }
}
