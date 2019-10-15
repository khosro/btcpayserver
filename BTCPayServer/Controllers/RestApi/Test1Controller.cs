using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.FileManager;
using BTCPayServer.Data;
using BTCPayServer.Models.AccountViewModels;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenIddict.Validation;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using static BTCPayServer.Controllers.RestApi.JsonModelBinder;
using Newtonsoft.Json.Serialization;
using System.Collections;

namespace BTCPayServer.Controllers.RestApi
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [EnableCors(CorsPolicies.All)]
    [Authorize(AuthenticationSchemes = OpenIddictValidationDefaults.AuthenticationScheme)]
    public class Test1Controller : ControllerBase
    {
        ImageUploader _imageUploader;
        public Test1Controller(ImageUploader imageUploader)
        {
            _imageUploader = imageUploader;
        }

        [HttpGet("testaction")]
        public IActionResult Testaction()
        {
            var response = new SingleResponse<string>();
            response.Model = "test";
            return response.ToHttpResponse();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(/*IEnumerable<IFormFile> files[FromBody]Payload data*/
                                                    // [ModelBinder(BinderType = typeof(JsonModelBinder))]
           [ModelBinder(BinderType = typeof(JsonWithFilesFormDataModelBinder))]
           /* [FromForm] Payload payload,*/ [FromForm] Data data)
        {
            IEnumerable<IFormFile> files = HttpContext.Request.Form.Files;
            StringValues values;
            HttpContext.Request.Form.TryGetValue("data", out values);
            var response = new SingleResponse<string>();
            await _imageUploader.Upload(new AspNetCore.FileManager.Models.FileUploadModel() { File = files.FirstOrDefault(), Path = "test" });
            response.Model = "test";
            return response.ToHttpResponse();
        }
    }

    public class Payload
    {
        public Data data { get; set; }
        public string FirstName { get; set; }
        public IEnumerable<IFormFile> files { get; set; }
    }


    public class Data
    {
        public string FirstName { get; set; }
    }

    public class JsonModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // Check the value sent in
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

                // Attempt to convert the input value
                var valueAsString = valueProviderResult.FirstValue;
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject(valueAsString, bindingContext.ModelType);
                if (result != null)
                {
                    bindingContext.Result = ModelBindingResult.Success(result);
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }

        public class JsonWithFilesFormDataModelBinder : IModelBinder
        {
            private readonly IOptions<MvcJsonOptions> _jsonOptions;
            private readonly FormFileModelBinder _formFileModelBinder;

            public JsonWithFilesFormDataModelBinder(IOptions<MvcJsonOptions> jsonOptions, ILoggerFactory loggerFactory)
            {
                _jsonOptions = jsonOptions;
                _formFileModelBinder = new FormFileModelBinder(loggerFactory);
            }

            public async Task BindModelAsync(ModelBindingContext bindingContext)
            {
                if (bindingContext == null)
                    throw new ArgumentNullException(nameof(bindingContext));

                // Retrieve the form part containing the JSON
                var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.FieldName);
                if (valueResult == ValueProviderResult.None)
                {
                    // The JSON was not found
                    var message = bindingContext.ModelMetadata.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(bindingContext.FieldName);
                    bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, message);
                    return;
                }

                var rawValue = valueResult.FirstValue;

                // JsonSerializerSettings settings = new JsonSerializerSettings();
                // settings.ContractResolver = new DictionaryAsArrayResolver();

                // Deserialize the JSON
                var model = JsonConvert.DeserializeObject(rawValue, bindingContext.ModelType, _jsonOptions.Value.SerializerSettings);

                // Now, bind each of the IFormFile properties from the other form parts
                foreach (var property in bindingContext.ModelMetadata.Properties)
                {
                    if (property.ModelType != typeof(IFormFile))
                        continue;

                    var fieldName = property.BinderModelName ?? property.PropertyName;
                    var modelName = fieldName;
                    var propertyModel = property.PropertyGetter(bindingContext.Model);
                    ModelBindingResult propertyResult;
                    using (bindingContext.EnterNestedScope(property, fieldName, modelName, propertyModel))
                    {
                        await _formFileModelBinder.BindModelAsync(bindingContext);
                        propertyResult = bindingContext.Result;
                    }

                    if (propertyResult.IsModelSet)
                    {
                        // The IFormFile was sucessfully bound, assign it to the corresponding property of the model
                        property.PropertySetter(model, propertyResult.Model);
                    }
                    else if (property.IsBindingRequired)
                    {
                        var message = property.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(fieldName);
                        bindingContext.ModelState.TryAddModelError(modelName, message);
                    }
                }

                // Set the successfully constructed model as the result of the model binding
                bindingContext.Result = ModelBindingResult.Success(model);
            }
        }

        class DictionaryAsArrayResolver : DefaultContractResolver
        {
            protected override JsonContract CreateContract(Type objectType)
            {
                if (objectType.GetInterfaces().Any(i => i == typeof(IDictionary) ||
                   (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))))
                {
                    return base.CreateArrayContract(objectType);
                }

                return base.CreateContract(objectType);
            }
        }
    }

}
