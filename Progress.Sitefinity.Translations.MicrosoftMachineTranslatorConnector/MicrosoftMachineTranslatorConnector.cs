/**
Uses code from Microsoft-Document-Translator
https://github.com/MicrosoftTranslator/DocumentTranslator 
licensed under MIT license
https://github.com/MicrosoftTranslator/DocumentTranslator/blob/master/LICENSE.md

Microsoft-Document-Translator

Copyright (c) Microsoft Corporation

All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Progress.Sitefinity.Translations.MicrosoftMachineTranslatorConnector;
using Progress.Sitefinity.Translations.MicrosoftMachineTranslatorConnector.Exceptions;
using Telerik.Sitefinity.Translations;


[assembly: TranslationConnector(name: Constants.Name,
                                connectorType: typeof(MicrosoftMachineTranslatorConnector),
                                title: Constants.Title,
                                enabled: false,
                                removeHtmlTags: false,
                                parameters: new string[] { Constants.ConfigParameters.ApiKey,
                                    Constants.ConfigParameters.Region,
                                    Constants.ConfigParameters.BaseUrl,
                                    Constants.ConfigParameters.QueryString})]
namespace Progress.Sitefinity.Translations.MicrosoftMachineTranslatorConnector
{
    /// <summary>
    /// Connector for Microsoft Transaltor Text Service API
    /// </summary>
    public class MicrosoftMachineTranslatorConnector : MachineTranslationConnector
    {
        protected virtual HttpClient GetClient()
        {
            return new HttpClient();
        }

        /// <summary>
        /// Configures the connector instance
        /// </summary>
        /// <param name="config">apiKey key should contain the Azure Transaltor Text Api Service key.</param>
        protected override void InitializeConnector(NameValueCollection config)
        {
            var key = config.Get(Constants.ConfigParameters.ApiKey);
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(Constants.ExceptionMessages.NoApiKeyExceptionMessage);
            }

            if (key.Length != Constants.ValidApiKeyLength)
            {
                throw new ArgumentException(Constants.ExceptionMessages.InvalidApiKeyExceptionMessage);
            }

            this.key = key;

            var region = config.Get(Constants.ConfigParameters.Region);
            this.region = region;

            var baseURL = config.Get(Constants.ConfigParameters.BaseUrl);
            if (string.IsNullOrEmpty(baseURL))
            {
                baseURL = Constants.MicrosoftTranslatorEndpointConstants.DefaultEndpointUrl;
            }

            this.baseUrl = baseURL;

            var queryString = config.Get(Constants.ConfigParameters.QueryString);
            this.queryString = queryString;
        }

        protected override List<string> Translate(List<string> input, ITranslationOptions translationOptions)
        {
            if (translationOptions == null)
            {
                throw new ArgumentException(GetTranslаteArgumentExceptionMessage(nameof(translationOptions)));
            }

            var fromLanguageCode = translationOptions.SourceLanguage;
            var toLanguageCode = translationOptions.TargetLanguage;

            if (string.IsNullOrWhiteSpace(fromLanguageCode))
            {
                throw new ArgumentException(GetTranslаteArgumentExceptionMessage($"{nameof(translationOptions)}.{nameof(translationOptions.SourceLanguage)}"));
            }

            if (string.IsNullOrWhiteSpace(toLanguageCode))
            {
                throw new ArgumentException(GetTranslаteArgumentExceptionMessage($"{nameof(translationOptions)}.{nameof(translationOptions.TargetLanguage)}"));
            }

            if (input == null || input.Count == 0)
            {
                throw new ArgumentException(GetTranslаteArgumentExceptionMessage(nameof(input)));
            }

            if (fromLanguageCode == toLanguageCode)
            {
                return input;
            }

            bool translateIndividually = false;

            // Because the connector does not removeHtmlTags is set to false, we expect to have only 1 input
            foreach (string text in input)
            {
                if (!string.IsNullOrWhiteSpace(text) && text.Length >= MaxTranslateRequestSize) translateIndividually = true;
            }
            if (translateIndividually)
            {
                List<string> resultlist = new List<string>();
                foreach (string text in input)
                {
                    List<string> splitstring = SplitString(text, fromLanguageCode);
                    var linetranslation = new StringBuilder();
                    foreach (string innerText in splitstring)
                    {
                        var hasTrailingWhiteSpace = innerText.EndsWith(" ");
                        var innertranslation = TranslateCore(new List<string> { innerText }, translationOptions);
                        linetranslation.Append(innertranslation[0]);
                        // The translator trims whitespaces by default, so add space if it was there before
                        if (hasTrailingWhiteSpace)
                        {
                            linetranslation.Append(" ");
                        }
                    }
                    resultlist.Add(linetranslation.ToString());
                }
                return resultlist.ToList();
            }
            else
            {
                return TranslateCore(input, translationOptions);
            }
        }

        private List<string> TranslateCore(List<string> input, ITranslationOptions translationOptions)
        {
            int currentRetry = 0;
            var translations = new List<string>();

            while (true)
            {
                try
                {
                    translations = TryTranslate(input, translationOptions);
                    break;
                }
                catch (Exception ex)
                {
                    currentRetry++;

                    if (currentRetry > Constants.SendTranslationRetryCount)
                    {
                        throw ex;
                    }
                }
            }

            return translations;
        }

        private List<string> TryTranslate(List<string> input, ITranslationOptions translationOptions)
        {
            var fromLanguageCode = translationOptions.SourceLanguage;
            var toLanguageCode = translationOptions.TargetLanguage;
            string uri = GetAzureTranslateEndpointUri(fromLanguageCode, toLanguageCode);

            var body = new List<object>();
            foreach (var text in input)
            {
                body.Add(new { Text = text ?? string.Empty });
            }

            var serializer = new JavaScriptSerializer();
            string requestBody = serializer.Serialize(body);

            using (var client = this.GetClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", this.key);
                if (!string.IsNullOrWhiteSpace(this.region))
                {
                    request.Headers.Add("Ocp-Apim-Subscription-Region", this.region);
                }
                request.Headers.Add("X-ClientTraceId", Guid.NewGuid().ToString());

                var response = new HttpResponseMessage();
                var responseTask = client.SendAsync(request).ContinueWith(r => response = r.Result);
                responseTask.Wait();

                var responseBody = string.Empty;
                var responseBodyTask = response.Content.ReadAsStringAsync().ContinueWith(r => responseBody = r.Result);
                responseBodyTask.Wait();

                if (!response.IsSuccessStatusCode)
                {
                    this.HandleApiError(responseBody, response);
                }

                dynamic result;
                try
                {
                    result = serializer.DeserializeObject(responseBody);
                }
                catch (Exception ex)
                {
                    if (IsSerializationException(ex))
                    {
                        throw new MicrosoftTranslatorConnectorSerializationException($"{Constants.ExceptionMessages.ErrorSerializingResponseFromServer} Server response: {response.StatusCode} {response.ReasonPhrase} {responseBody}");
                    }

                    throw;
                }

                var translations = new List<string>();
                try
                {
                    for (int i = 0; i < input.Count(); i++)
                    {
                        // currently Sitefinity does not support sending multiple languages at once, only multiple strings
                        var translation = result[i]["translations"][0]["text"];
                        translations.Add(translation);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is KeyNotFoundException || ex is NullReferenceException)
                    {
                        throw new MicrosoftTranslatorConnectorResponseFormatException($"{Constants.ExceptionMessages.UnexpectedResponseFormat} Server response: {response.StatusCode} {response.ReasonPhrase} {responseBody}");
                    }

                    throw;
                }

                return translations;
            }
        }

        private string GetAzureTranslateEndpointUri(string fromLanguageCode, string toLanguageCode)
        {
            string uri = string.Format(
                $"{this.baseUrl}{Constants.MicrosoftTranslatorEndpointConstants.TranslatorPathAndVersion}&{Constants.MicrosoftTranslatorEndpointConstants.SourceCultureQueryParam }={{0}}&{Constants.MicrosoftTranslatorEndpointConstants.TargetCultureQueryParam }={{1}}",
                fromLanguageCode,
                toLanguageCode);

            if (!IsRemoveHtmlTagsEnabled())
                uri += $"&{Constants.MicrosoftTranslatorEndpointConstants.TextTypeQueryParam}=html";

            if (!string.IsNullOrEmpty(this.queryString))
            {
                if (this.queryString.StartsWith("?") || this.queryString.StartsWith("&"))
                {
                    this.queryString = this.queryString.Remove(0, 1);
                }

                uri += $"&{this.queryString}";
            }

            return uri;
        }

        protected virtual bool IsRemoveHtmlTagsEnabled()
        {
            return this.RemoveHtmlTags;
        }

        private static string GetTranslаteArgumentExceptionMessage(string paramName)
        {
            return string.Format(Constants.ExceptionMessages.InvalidParameterForMicrosoftTransaltionRequestExceptionMessageTemplate, paramName);
        }

        private void HandleApiError(string responseBody, HttpResponseMessage response)
        {
            var serializer = new JavaScriptSerializer();
            dynamic jsonResponse;
            try
            {
                jsonResponse = serializer.DeserializeObject(responseBody);
            }
            catch (Exception ex)
            {
                if (IsSerializationException(ex))
                {
                    throw new MicrosoftTranslatorConnectorSerializationException($"{Constants.ExceptionMessages.ErrorSerializingErrorResponseFromServer} Server response: {response.StatusCode} {response.ReasonPhrase} {responseBody}");
                }

                throw;
            }

            try
            {
                throw new MicrosoftTranslatorConnectorException(jsonResponse["error"]["message"]);
            }
            catch (Exception ex)
            {
                if (ex is KeyNotFoundException || ex is NullReferenceException)
                {
                    throw new MicrosoftTranslatorConnectorResponseFormatException($"{Constants.ExceptionMessages.UnexpectedErrorResponseFormat} Server response: {response.StatusCode} {response.ReasonPhrase} {responseBody}");
                }

                throw;
            }
        }

        private static bool IsSerializationException(Exception ex)
        {
            return ex is ArgumentException || ex is ArgumentNullException || ex is InvalidOperationException;
        }

        private string key;


        #region code _Adapted_ from https://github.com/MicrosoftTranslator/DocumentTranslator/blob/5cbf1f69e94c249527772ac14d28eea8594a832e/TranslationServices.Core/TranslationServiceFacade.cs

        // based on current limits as of 27 august 2024
        // https://learn.microsoft.com/en-us/azure/ai-services/Translator/service-limits
        public static readonly int MaxTranslateRequestSize = 50000;

        /// <summary>
        /// Split a string > than <see cref="MaxTranslateRequestSize"/> into a list of smaller strings, at the appropriate sentence breaks. 
        /// </summary>
        /// <param name="text">The text to split.</param>
        /// <param name="languagecode">The language code to apply.</param>
        /// <returns>List of strings, each one smaller than maxrequestsize</returns>
        private List<string> SplitString(string text, string languagecode)
        {
            List<string> result = new List<string>();
            int previousboundary = 0;
            if (text.Length <= MaxTranslateRequestSize)
            {
                result.Add(text);
            }
            else
            {
                while (previousboundary <= text.Length)
                {
                    int boundary = LastSentenceBreak(text.Substring(previousboundary), languagecode);
                    if (boundary == 0) break;
                    result.Add(text.Substring(previousboundary, boundary));
                    previousboundary += boundary;
                }
                result.Add(text.Substring(previousboundary));
            }
            return result;
        }

        /// <summary>
        /// Returns the last sentence break in the text.
        /// </summary>
        /// <param name="text">The original text</param>
        /// <param name="languagecode">A language code</param>
        /// <returns>The offset of the last sentence break, from the beginning of the text.</returns>
        private int LastSentenceBreak(string text, string languagecode)
        {
            int sum = 0;
            List<int> breakSentenceResult = BreakSentences(text, languagecode);
            for (int i = 0; i < breakSentenceResult.Count - 1; i++) sum += breakSentenceResult[i];
            return sum;
        }


        /// <summary>
        /// Breaks string into sentences. The string will be cut off at maxrequestsize. 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="language"></param>
        /// <returns>List of integers denoting the offset of the sentence boundaries</returns>
        public List<int> BreakSentences(string text, string languagecode)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrWhiteSpace(text)) return null;
            string path = "/breaksentence?api-version=3.0";
            string params_ = "&language=" + languagecode;
            string uri = this.baseUrl + path + params_;
            object[] body = new object[] { new { Text = text.Substring(0, (text.Length < MaxTranslateRequestSize) ? text.Length : MaxTranslateRequestSize) } };
            string requestBody = JsonConvert.SerializeObject(body);
            List<int> resultList = new List<int>();

            using (var client = this.GetClient())
            using (HttpRequestMessage request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", this.key);
                if (!string.IsNullOrWhiteSpace(this.region))
                {
                    request.Headers.Add("Ocp-Apim-Subscription-Region", this.region);
                }

                var response = new HttpResponseMessage();
                var responseTask = client.SendAsync(request).ContinueWith(r => response = r.Result);
                responseTask.Wait();

                var result = string.Empty;
                var resultTask = response.Content.ReadAsStringAsync().ContinueWith(r => result = r.Result);
                resultTask.Wait();

                if (!response.IsSuccessStatusCode)
                {
                    this.HandleApiError(result, response);
                }

                BreakSentenceResult[] deserializedOutput = JsonConvert.DeserializeObject<BreakSentenceResult[]>(result);
                foreach (BreakSentenceResult o in deserializedOutput)
                {
                    //Console.WriteLine("The detected language is '{0}'. Confidence is: {1}.", o.DetectedLanguage.Language, o.DetectedLanguage.Score);
                    //Console.WriteLine("The first sentence length is: {0}", o.SentLen[0]);
                    resultList = o.SentLen.ToList();
                }
            }
            return resultList;
        }

        // Used in the BreakSentences method.
        private class BreakSentenceResult
        {
            public int[] SentLen { get; set; }
            public DetectedLanguage DetectedLanguage { get; set; }
        }

        private class DetectedLanguage
        {
            public string Language { get; set; }
            public float Score { get; set; }
        }

        #endregion

        private string region;
        private string baseUrl;
        private string queryString;
    }
}