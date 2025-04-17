Machine translation connector for Microsoft Translator
===========================================

>**Latest supported version**: Sitefinity CMS 15.3.8500.0

>**Documentation articles**: [Custom translation connector](http://www.progress.com/documentation/sitefinity-cms/custom-translation-connector)

>**IMPORTANT**: This repository may not be compatible with the latest or your current Sitefinity CMS version. If you want to use the repository with a specific Sitefinity CMS version, either upgrade the code from this repository or your Sitefinity CMS project to ensure compatibility.<br/>
The dev team monitors the repository. You can create a GitHub issue to submit feedback or report bugs. Or make a pull request to submit project enhancements or compatibility changes that support new Sitefinity CMS versions.

### Overview
In addition to the built-in *Translation* module connectors, you can implement your own translation connector with custom logic to serve your requirements.

This repo contains a translation connector to work with the Microsoft Azure [Translator](https://azure.microsoft.com/en-us/services/cognitive-services/translator-text-api/) V3 API service. 

### Prerequisites
- You must have a Sitefinity CMS license.
- Your setup must comply with the system requirements.
 For more information, see the [System requirements](https://docs.sitefinity.com/system-requirements) for the respective Sitefinity CMS version.
- You must sign up for Microsoft Translator Text API V3 service in Microsoft Azure.
 For more information, see [How to sign up for Translator](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/translator-text-how-to-signup).
 Then you can use one of the generated API keys to configure the connector in Sitefinity CMS. All of the available tiers are supported.
 
### Installation
Add the Microsoft translation connector sample project to your solution.
 To do this, perform the following:
1. Open your Sitefinity CMS solution in Visual Studio.
2. Add `Progress.Sitefinity.Translations.MicrosoftMachineTranslatorConnector` project to the same solution.
3. Ensure `Telerik.Sitefinity.Translations` nuget package is installed in _Progress.Sitefinity.Translations.MicrosoftMachineTranslatorConnector_.
4. In _SitefinityWebApp_, add a reference to the _Progress.Sitefinity.Translations.MicrosoftMachineTranslatorConnector_ project.
5. Build your solution.

## Configure the connector
Using one of the generated API keys, configure the connector in the following way:
1. In Sitefinity CMS backend, navigate to _Administration » Settings » Advanced_.
2. In the treeview on the left, expand _Translations » Connectors » MicrosoftMachineTranslatorConnector » Parameters_.
3. Set the _apiKey_ parameter to the API key provided by Microsoft Azure.
4. Set the _baseURL_ to one of the URL’s supported by Microsoft Azure API.
 For more information, see _Microsoft Azure documentation » Translator v3.0 »_ [Base URL's](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/reference/v3-0-reference#base-urls).
 If you do not set the baseURL parameter, the system takes the _Global (non-regional)_ base URL.
5. Set the _region_ parameter to one of the regions, supported by Microsoft Azure API.
 If you have not changed the _baseURL_ parameter from the default one, you do not need to configure the _region_ parameter.
 For more information, see _Microsoft Azure documentation » Authenticate requests to Azure Cognitive Services »_ [Supported regions](https://docs.microsoft.com/en-us/azure/cognitive-services/authentication?tabs=powershell#supported-regions).
 
   >**IMPORTANT**: The _Azure geography_ of the base URL must include the region that you have configured.
6. You may optionally configure any additional parameter of the Microsoft Translator service described in [Translator 3.0: Translate](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/reference/v3-0-translate#optional-parameters). To do this, click _queryString_ on the left. In the _Value_ field on the right enter the parameters.
 
   >**IMPORTANT**: You format the string in the _Value_ field using the [query string](https://en.wikipedia.org/wiki/Query_string) format without the leading _?_ or _&amp;_ characters. You separate the translator parameters with _&amp;_.
7. Navigate back to _MicrosoftMachineTranslatorConnector_.
8. Select _Enabled_ and deselect _Strip HTML tags_.
9. Save your changes.

### Additional resources

For more information for supported languages, see [Language and region support for text and speech translation](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/language-support#translation).