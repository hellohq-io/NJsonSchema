//-----------------------------------------------------------------------
// <copyright file="CSharpTypeResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Manages the generated types and converts JSON types to CSharp types. </summary>
    public class TypeScriptTypeResolver : TypeResolverBase<TypeScriptInterfaceGenerator>
    {
        /// <summary>Initializes a new instance of the <see cref="TypeScriptTypeResolver"/> class.</summary>
        public TypeScriptTypeResolver()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptTypeResolver"/> class.</summary>
        /// <param name="knownSchemes">The known schemes.</param>
        public TypeScriptTypeResolver(JsonSchema4[] knownSchemes)
        {
            foreach (var type in knownSchemes)
                AddTypeGenerator(type.TypeName, new TypeScriptInterfaceGenerator(type.ActualSchema, this));
        }

        /// <summary>Gets or sets the namespace of the generated classes.</summary>
        public string Namespace { get; set; }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isRequired">Specifies whether the given type usage is required.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        public override string Resolve(JsonSchema4 schema, bool isRequired, string typeNameHint)
        {
            schema = schema.ActualSchema;

            var type = schema.Type;
            if (type.HasFlag(JsonObjectType.Array))
            {
                var property = schema;
                if (property.Item != null)
                    return string.Format("{0}[]", Resolve(property.Item, true, null));

                throw new NotImplementedException("Items not supported");
            }

            if (type.HasFlag(JsonObjectType.Number))
                return "number";

            if (type.HasFlag(JsonObjectType.Integer))
                return "number";

            if (type.HasFlag(JsonObjectType.Boolean))
                return "boolean";

            if (type.HasFlag(JsonObjectType.String))
            {
                if (schema.Format == JsonFormatStrings.DateTime)
                    return "Date";
                else
                    return "string";
            }

            if (type.HasFlag(JsonObjectType.Object))
            {
                if (schema.IsDictionary)
                    return string.Format("{{ [key: string] : {0}; }}", Resolve(schema.AdditionalPropertiesSchema, true, null));

                var typeName = GetOrGenerateTypeName(schema, typeNameHint);
                if (!HasTypeGenerator(typeName))
                {
                    var generator = new TypeScriptInterfaceGenerator(schema, this);
                    AddTypeGenerator(typeName, generator);
                }
                return typeName;
            }

            throw new NotImplementedException("Type not supported");
        }

        /// <summary>Generates the interfaces.</summary>
        /// <returns>The interfaces.</returns>
        public string GenerateInterfaces()
        {
            var classes = "";
            var runGenerators = new List<TypeScriptInterfaceGenerator>();
            while (Types.Any(t => !runGenerators.Contains(t)))
                classes += GenerateInterfaces(runGenerators);
            return classes;
        }

        private string GenerateInterfaces(List<TypeScriptInterfaceGenerator> runGenerators)
        {
            var classes = "";
            foreach (var type in Types.Where(t => !runGenerators.Contains(t)))
            {
                classes += type.GenerateInterface() + "\n\n";
                runGenerators.Add(type);
            }
            return classes;
        }
    }
}