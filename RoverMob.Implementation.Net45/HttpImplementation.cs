﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace RoverMob.Implementation
{
    public static class HttpImplementation
    {
        public static async Task<HttpProxy> CreateProxyAsync(string accessToken)
        {
            HttpClient client = new HttpClient();
            if (accessToken != null)
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer", accessToken);
            }
            return new HttpProxy(client);
        }
    }
}
