﻿using System;
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

            foreach (var validationError in validationErrors)
            {
                message += $"\r\n\tLocalId: {validationError.LocalId}";
                
                foreach ( var errorText in validationError.ErrorTexts)
                    message +=
                        $"\r\n\r\n{errorText}";

                message += "\r\n\r\n----------------------------------------------------------------\r\n";
            }

            
            
            return message;
        }

        private static List<ValidationError> GetValidationErrors(XDocument changelogXml)
        {
            var schemas = GetSchemas(changelogXml);

            var validationErrors = new List<ValidationError>();

            Console.WriteLine("INFO: Starting validation");

            changelogXml.Validate(schemas, (o, e) =>
            {
                var parent = GetParent(o);

                var localId = parent.Descendants().First(n => n.Name.LocalName == "lokalId").Value;

                if (validationErrors.Any(v => v.LocalId == localId))
                    validationErrors.First(v => v.LocalId == localId).ErrorTexts.Add(e.Message);
                
                else
                    validationErrors.Add(new ValidationError
                    {
                        Element = parent,
                        LocalId = localId,
                        ErrorTexts = new List<string> {e.Message}
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

                Console.WriteLine($"INFO: Adding schema {schemaLocationSplit[i + 1]}");

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
            public List<string> ErrorTexts { get; set; }
            public string LocalId { get; set; }
        }
    }
}