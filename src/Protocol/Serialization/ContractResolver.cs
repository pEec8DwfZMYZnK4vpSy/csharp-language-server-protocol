using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace OmniSharp.Extensions.LanguageServer.Protocol.Serialization
{
    class ContractResolver : DefaultContractResolver
    {
        private readonly CompletionItemKind[] _completionItemKinds;
        private readonly SymbolKind[] _documentSymbolKinds;
        private readonly SymbolKind[] _workspaceSymbolKinds;

        public ContractResolver(
            CompletionItemKind[] completionItemKinds,
            SymbolKind[] documentSymbolKinds,
            SymbolKind[] workspaceSymbolKinds)
        {
            _completionItemKinds = completionItemKinds;
            _documentSymbolKinds = documentSymbolKinds;
            _workspaceSymbolKinds = workspaceSymbolKinds;
            NamingStrategy = new CamelCaseNamingStrategy(true, false, true);
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);
            if (objectType == typeof(WorkspaceClientCapabilites) ||
                objectType == typeof(TextDocumentClientCapabilities))
            {
                foreach (var property in contract.Properties)
                {
                    var isSupportedGetter = property.PropertyType.GetTypeInfo()
                        .GetProperty(nameof(Supports<object>.IsSupported), BindingFlags.Public | BindingFlags.Instance);
                    property.NullValueHandling = NullValueHandling.Ignore;
                    property.GetIsSpecified = o => {
                        var propertyValue = property.ValueProvider.GetValue(o);
                        if (propertyValue == null) return false;
                        return isSupportedGetter.GetValue(propertyValue) as bool? == true;
                    };
                }
            }

            return contract;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (member.GetCustomAttributes<OptionalAttribute>().Any()
             || property.DeclaringType.Name.EndsWith("Capabilities")
            )
            {
                property.NullValueHandling = NullValueHandling.Ignore;
                // property.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
            }

            if (property.DeclaringType == typeof(CompletionItem) && property.PropertyType == typeof(CompletionItemKind))
            {
                property.ValueProvider = new RangeValueProvider<CompletionItemKind>(property.ValueProvider, _completionItemKinds);
            }

            if (property.DeclaringType == typeof(DocumentSymbolInformation) && property.PropertyType == typeof(SymbolKind))
            {
                property.ValueProvider = new RangeValueProvider<SymbolKind>(property.ValueProvider, _documentSymbolKinds);
            }

            if (property.DeclaringType == typeof(WorkspaceSymbolInformation) && property.PropertyType == typeof(SymbolKind))
            {
                property.ValueProvider = new RangeValueProvider<SymbolKind>(property.ValueProvider, _workspaceSymbolKinds);
            }

            return property;
        }

        class RangeValueProvider<T> : IValueProvider
            where T : struct
        {
            private readonly IValueProvider _valueProvider;
            private readonly T[] _validValues;
            private readonly T _defaultValue;

            public RangeValueProvider(IValueProvider valueProvider, T[] validValues)
            {
                _valueProvider = valueProvider;
                _validValues = validValues;
                _defaultValue = validValues[0];
            }

            public void SetValue(object target, object value)
            {
                _valueProvider.SetValue(target, value);
            }

            public object GetValue(object target)
            {
                var value = (T) _valueProvider.GetValue(target);
                return _validValues.Any(z => z.Equals(value)) ? value : _defaultValue;
            }
        }
    }
}
