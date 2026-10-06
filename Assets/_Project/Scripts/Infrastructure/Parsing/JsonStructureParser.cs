using System;
using StructureViewer.Application.Loading;
using UnityEngine;

namespace StructureViewer.Infrastructure.Parsing
{
    public sealed class JsonStructureParser : IStructureParser
    {
        public ParseResult Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return ParseResult.Fail(new[] { "File is empty." });

            StructureDto dto;
            try
            {
                dto = JsonUtility.FromJson<StructureDto>(json);
            }
            catch (ArgumentException e)
            {
                return ParseResult.Fail(new[] { $"Invalid JSON: {e.Message}" });
            }
            return StructureDtoConverter.Convert(dto);
        }
    }
}
