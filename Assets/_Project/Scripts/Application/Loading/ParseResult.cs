using System;
using System.Collections.Generic;
using StructureViewer.Domain.Structure;

namespace StructureViewer.Application.Loading
{
    public sealed class ParseResult
    {
        private ParseResult(StructureModel model, IReadOnlyList<string> errors)
        {
            Model = model;
            Errors = errors;
        }

        // Null when parsing failed.
        public StructureModel Model { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool Success => Model != null;

        public static ParseResult Ok(StructureModel model) =>
            new ParseResult(model ?? throw new ArgumentNullException(nameof(model)), Array.Empty<string>());

        public static ParseResult Fail(IReadOnlyList<string> errors)
        {
            if (errors == null || errors.Count == 0)
                throw new ArgumentException("A failed parse needs at least one error.", nameof(errors));
            return new ParseResult(null, errors);
        }
    }
}
