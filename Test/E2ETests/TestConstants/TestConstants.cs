﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TestUtils.TestConstants
{
    public class TestConstants
    {
        public const string WebAppInstrumentationKey = "e45209bb-49ab-41a0-8065-793acb3acc56";
        public const string WebAppCore20NameInstrumentationKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8";
        public const string WebApiInstrumentationKey = "0786419e-d901-4373-902a-136921b63fb2";

        public const string WebAppName = "WebApp";
        public const string WebAppCore20Name = "WebAppCore20";
        public const string WebApiName = "WebApi";
        public const string IngestionName = "Ingestion";

        public const string WebAppImageName = "e2etests_e2etestwebapp";
        public const string WebAppCore20ImageName = "e2etests_e2etestwebappcore20";
        public const string WebApiImageName = "e2etests_e2etestwebapi";
        public const string IngestionImageName = "e2etests_ingestionservice";

        public const string WebAppContainerName = WebAppImageName + "_1";
        public const string WebAppCore20ContainerName = WebAppCore20ImageName + "_1";
        public const string WebApiContainerName = WebApiImageName + "_1";
        public const string IngestionContainerName = IngestionImageName + "_1";

        public const string WebAppHealthCheckPath = "/Dependencies?type=etw";
        public const string WebAppCore20HealthCheckPath = "/api/values";
        public const string WebApiHealthCheckPath = "/api/values";
        public const string IngestionHealthCheckPath = "/api/Data/HealthCheck?name=cijo";

        public const string WebAppFlushPath = "/Dependencies?type=flush";
        public const string WebAppCore20FlushPath = "/external/calls?type=flush";
        public const string WebApiFlushPath = "/api/values";
        public const string IngestionFlushPath = "/api/Data/HealthCheck?name=cijo";

    }
}
