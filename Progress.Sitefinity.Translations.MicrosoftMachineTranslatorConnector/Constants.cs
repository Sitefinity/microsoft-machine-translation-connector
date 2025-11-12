namespace Progress.Sitefinity.Translations.MicrosoftMachineTranslatorConnector
{
    internal class Constants
    {
        public const string Name = "MicrosoftMachineTranslatorConnector";
        public const string Title = "Machine translation connector for Microsoft Translator";
        public const int SendTranslationRetryCount = 2;

        internal class ExceptionMessages
        {
            public const string InvalidApiKeyExceptionMessage = "Invalid API subscription key.";
            public const string NoApiKeyExceptionMessage = "No API key configured for machine translation connector for Microsoft Translator.";
			public const string InvalidParameterForMicrosoftTransaltionRequestExceptionMessagePrefix = "Invalid parameter for Microsoft Translator request.";
            public const string NullOrEmptyParameterExceptionMessageTemplate = "Parameter with name {0} cannot be null or empty.";
            public static readonly string InvalidParameterForMicrosoftTransaltionRequestExceptionMessageTemplate = InvalidParameterForMicrosoftTransaltionRequestExceptionMessagePrefix + " " + NullOrEmptyParameterExceptionMessageTemplate;

            public static readonly string UnexpectedErrorResponseFormat = $"{MicrosoftServerErrorMessage} {UnexpectedResponseFormat}";
            public static readonly string ErrorSerializingErrorResponseFromServer = $"{MicrosoftServerErrorMessage} {ErrorSerializingResponseFromServer}";
            public static readonly string ErrorSerializingResponseFromServer = "Could not serialize response from Microsoft Translator service.";
            public const string UnexpectedResponseFormat = "The response received was not in the expected format.";

            public const string MicrosoftServerErrorMessage = "An error ocurred on the Microsoft server.";
        }

        internal struct ConfigParameters
        {
			public const string BaseUrl = "baseURL";
            public const string ApiKey = "apiKey";
			public const string Region = "region";
			public const string QueryString = "queryString";
        }

        internal struct MicrosoftTranslatorEndpointConstants
        {
            public const string DefaultEndpointUrl = "https://api.cognitive.microsofttranslator.com";
            public const string TranslatorPathAndVersion = "/translate?api-version=3.0";
            public const string TextTypeQueryParam = "textType";
            public const string TargetCultureQueryParam = "to";
            public const string SourceCultureQueryParam = "from";
        }
    }
}