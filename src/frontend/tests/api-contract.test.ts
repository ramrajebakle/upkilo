import { PactV3, MatchersV3 } from '@pact-foundation/pact';
import axios from 'axios';

const provider = new PactV3({
  consumer: 'UpkiloFrontend',
  provider: 'UpkiloBackend',
  dir: './pacts'
});

describe('Upkilo API Contract Tests', () => {
  it('should verify booking slot retrieval schema contract', async () => {
    provider
      .given('there are available time slots')
      .uponReceiving('a request for availability')
      .withRequest({
        method: 'GET',
        path: '/api/v1/bookings/availability',
        query: { serviceId: 'b5b5b5b5-b5b5-b5b5-b5b5-b5b5b5b5b5b5' },
      })
      .willRespondWith({
        status: 200,
        headers: { 'Content-Type': 'application/json' },
        body: MatchersV3.like({
          slots: MatchersV3.eachLike({
            time: '09:00',
            available: true,
          }),
        }),
      });

    await provider.executeTest(async (mockService) => {
      const response = await axios.get(`${mockService.port}/api/v1/bookings/availability`, {
        params: { serviceId: 'b5b5b5b5-b5b5-b5b5-b5b5-b5b5b5b5b5b5' },
      });
      expect(response.status).toEqual(200);
      expect(response.data.slots[0].time).toEqual('09:00');
    });
  });
});
