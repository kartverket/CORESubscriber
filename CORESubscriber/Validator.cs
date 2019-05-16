using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using CORESubscriber.Xml;

namespace CORESubscriber
{
    public class Validator
    {
        public static void Validate(XDocument changelogXml)
        {
            var validationErrors = GetValidationErrors(changelogXml);

            if (validationErrors.Count <= 0) return;

            throw new ValidationException(GetErrorMessage(validationErrors));
        }

        private static string GetErrorMessage(List<ValidationError> validationErrors)
        {
            var message =
                "ERROR: Validation failed. Listing errors\r\n----------------------------------------------------------------\r\n";

            validationErrors.ForEach(error => message += $"\r\n\tLocalId: {error.LocalId}\r\n\r\n{error.ErrorText}\r\n\r\n----------------------------------------------------------------\r\n");
            
            return message;
        }

        private static List<ValidationError> GetValidationErrors(XDocument changelogXml)
        {
            var schemas = GetSchemas(changelogXml);

            var validationErrors = new List<ValidationError>();

            changelogXml.Validate(schemas, (o, e) =>
            {
                var parent = GetParent(o);

                var identifikasjon = parent.Descendants().First(n => n.Name.LocalName == "lokalId").Value;

                validationErrors.Add(new ValidationError
                {
                    Element = parent,
                    LocalId = identifikasjon,
                    ErrorText = e.Message
                });
            });

            return validationErrors;
        }

        private static XmlSchemaSet GetSchemas(XDocument changelogXml)
        {
            var schemas = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };

            var schemaLocationSplit = changelogXml.Root?.Attributes().First(a => a.Name.LocalName == "schemaLocation").Value.Split(" ");

            if (schemaLocationSplit == null) return schemas;

            for (var i = 0; i <= schemaLocationSplit.Length / 2; i = i + 2)
            {
                if (schemaLocationSplit[i] == XmlNamespaces.Changelog) continue;

                schemas.Add(XmlSchema.Read(new XmlTextReader(schemaLocationSplit[i + 1]), (sender, args) => { }));
            }

            return schemas;
        }

        private static XElement GetParent(object o)
        {
            var parent = ((XElement)o).Parent;

            return parent != null && parent.Elements().Any(e => e.Name.LocalName == "identifikasjon") ? parent : GetParent(parent);
        }

        internal class ValidationError
        {
            public XElement Element { get; set; }
            public string ErrorText { get; set; }
            public string LocalId { get; set; }
        }
    }
}