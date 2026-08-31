#nullable enable

using System.Collections.Generic;
using System.Linq;
using com.amari_noa.unity_agent_framework.sdk.contracts;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>Standard pagination arguments (design doc section 15).</summary>
    public sealed class AgentPageArgs
    {
        public int? Offset { get; set; }
        public int? Limit { get; set; }
        public int? Depth { get; set; }
    }

    /// <summary>
    /// Applies the standard pagination rules (decision 3):
    /// default limit 100, max limit 1000 (INVALID_ARGUMENT beyond), default depth 1.
    /// </summary>
    public static class AgentPagination
    {
        public const int DefaultLimit = 100;
        public const int MaxLimit = 1000;
        public const int DefaultDepth = 1;

        /// <summary>Returns an error when the arguments violate the limits, otherwise null.</summary>
        public static AgentError? Validate(AgentPageArgs? args)
        {
            var limit = args?.Limit ?? DefaultLimit;
            var offset = args?.Offset ?? 0;
            if (limit < 1 || limit > MaxLimit || offset < 0)
            {
                return new AgentError
                {
                    Code = AgentErrorCodes.InvalidArgument,
                    Message = $"limit must be 1-{MaxLimit} and offset must be >= 0 (limit: {limit}, offset: {offset}).",
                    Provider = "core",
                    Retryable = false,
                };
            }

            return null;
        }

        /// <summary>Slice the source list and produce the matching PageInfo.</summary>
        public static List<T> Slice<T>(IReadOnlyList<T> source, AgentPageArgs? args, out PageInfo page)
        {
            var offset = args?.Offset ?? 0;
            var limit = args?.Limit ?? DefaultLimit;
            var items = source.Skip(offset).Take(limit).ToList();
            page = new PageInfo
            {
                Offset = offset,
                Limit = limit,
                Total = source.Count,
                HasMore = offset + items.Count < source.Count,
            };
            return items;
        }
    }
}
