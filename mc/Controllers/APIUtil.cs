﻿using DecisionServicePrivateWeb.Classes;
using Microsoft.Research.MultiWorldTesting.ClientLibrary;
using Microsoft.Research.MultiWorldTesting.Contract;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DecisionServicePrivateWeb.Controllers
{
    internal static class APIUtil
    {
        private const string AuthHeaderName = "auth";

        internal static string Authenticate(HttpRequestBase request, string token = null)
        {
            if (token == null)
                token = ConfigurationManager.AppSettings[ApplicationMetadataStore.AKWebServiceToken];

            var authToken = request.Headers[AuthHeaderName];

            if (authToken == null)
                throw new UnauthorizedAccessException("AuthorizationToken missing");

            if (string.IsNullOrWhiteSpace(authToken))
                throw new UnauthorizedAccessException("AuthorizationToken missing");

            if (authToken != token)
                throw new UnauthorizedAccessException();

            return token;
        }

        internal static string ReadBody(HttpRequestBase request)
        {
            var req = request.InputStream;
            req.Seek(0, System.IO.SeekOrigin.Begin);
            return new StreamReader(req).ReadToEnd();
        }

        internal static string CreateEventId()
        {
            return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        }

        internal static string GetSettingsUrl()
        {
            var settingsURL = ConfigurationManager.AppSettings[ApplicationMetadataStore.AKDecisionServiceSettingsUrl];
            if (settingsURL == null)
            {
                var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings[ApplicationMetadataStore.AKConnectionString]);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var settingsBlobContainer = blobClient.GetContainerReference(ApplicationBlobConstants.SettingsContainerName);
                var extraSettingsBlob = settingsBlobContainer.GetBlockBlobReference(ApplicationBlobConstants.LatestExtraSettingsBlobName);
                var extraSettings = JsonConvert.DeserializeObject<ApplicationExtraMetadata>(extraSettingsBlob.DownloadText());
                settingsURL = extraSettings.SettingsTokenUri1;
                ConfigurationManager.AppSettings.Set(ApplicationMetadataStore.AKDecisionServiceSettingsUrl, settingsURL);
            }
            return settingsURL;
        }
    }
}