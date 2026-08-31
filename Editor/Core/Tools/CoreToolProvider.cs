#nullable enable

using System.Collections.Generic;
using com.amari_noa.unity_agent_framework.sdk;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.amari_noa.unity_agent_framework.core.editor.tools
{
    /// <summary>
    /// Generic read-only tools shipped with the core (v0.1 scope, decision 6):
    /// unity.project.info / unity.scene.list / unity.object.inspect /
    /// unity.selection.get / unity.console.get. All tools are L0 (Mutation.None).
    /// </summary>
    [AgentToolProvider]
    public sealed class CoreToolProvider : IAgentToolProvider
    {
        private const string ProviderId = "core";
        private const string PackageId = "com.amari-noa.unity-agent-framework";

        private const string PageArgsSchema =
            "{\"type\":\"object\",\"properties\":{" +
            "\"offset\":{\"type\":\"integer\",\"minimum\":0}," +
            "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":1000}}}";

        public void RegisterTools(IAgentToolRegistry registry)
        {
            var version = ResolvePackageVersion();
            registry.RegisterProvider(new ProviderInfo
            {
                Id = ProviderId,
                DisplayName = "Unity Agent Core",
                PackageId = PackageId,
                PackageVersion = version,
                MinimumFrameworkVersion = version,
            });

            registry.RegisterTool(ReadDescriptor(
                    "unity.project.info",
                    "Static information about the open Unity project.",
                    "{\"type\":\"object\",\"properties\":{}}"),
                GetProjectInfo);

            registry.RegisterTool(ReadDescriptor(
                    "unity.scene.list",
                    "Lists the scenes currently loaded in the editor.",
                    PageArgsSchema),
                ListScenes);

            registry.RegisterTool(ReadDescriptor(
                    "unity.object.inspect",
                    "Resolves an object reference and returns its details.",
                    "{\"type\":\"object\",\"properties\":{\"ref\":{\"type\":\"object\"}},\"required\":[\"ref\"]}"),
                InspectObject);

            registry.RegisterTool(ReadDescriptor(
                    "unity.selection.get",
                    "Returns the current editor selection as object references.",
                    PageArgsSchema),
                GetSelection);

            var consoleDescriptor = ReadDescriptor(
                "unity.console.get",
                "Returns console messages collected since the editor loaded (ring buffer of "
                + AgentConsoleLogCollector.Capacity + " entries).",
                "{\"type\":\"object\",\"properties\":{" +
                "\"offset\":{\"type\":\"integer\",\"minimum\":0}," +
                "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":1000}," +
                "\"type\":{\"type\":\"string\",\"enum\":[\"log\",\"warning\",\"error\",\"assert\"]}}}");
            // The collector is thread-safe, so this read does not need the main thread.
            consoleDescriptor.ExecutionContext = AgentToolExecutionContext.Background;
            registry.RegisterTool(consoleDescriptor, GetConsole);
        }

        private static AgentToolDescriptor ReadDescriptor(string id, string description, string inputSchema)
        {
            return new AgentToolDescriptor
            {
                Id = id,
                Description = description,
                Provider = ProviderId,
                PackageId = PackageId,
                InputSchemaJson = inputSchema,
                ExecutionContext = AgentToolExecutionContext.MainThread,
                Mutation = AgentToolMutation.None,
                SupportsDryRun = false,
                RequiresConfirm = false,
                Undoable = false,
                ExportPolicy = AgentToolExportPolicy.Standalone,
            };
        }

        // ----- unity.project.info -----

        public sealed class ProjectInfoPayload
        {
            public string? ProjectName { get; set; }
            public string? ProjectPath { get; set; }
            public string? UnityVersion { get; set; }
            public string? ProductName { get; set; }
            public string? CompanyName { get; set; }
        }

        private static AgentResult<object> GetProjectInfo(AgentToolInvocation invocation)
        {
            var projectRoot = System.IO.Directory.GetParent(Application.dataPath)!.FullName;
            return Success(new ProjectInfoPayload
            {
                ProjectName = System.IO.Path.GetFileName(projectRoot),
                ProjectPath = projectRoot,
                UnityVersion = Application.unityVersion,
                ProductName = Application.productName,
                CompanyName = Application.companyName,
            });
        }

        // ----- unity.scene.list -----

        public sealed class SceneInfoPayload
        {
            public string? Name { get; set; }
            public string? Path { get; set; }
            public bool IsLoaded { get; set; }
            public bool IsDirty { get; set; }
            public bool IsActive { get; set; }
            public int RootCount { get; set; }
        }

        private static AgentResult<object> ListScenes(AgentToolInvocation invocation)
        {
            var args = invocation.GetParameters<AgentPageArgs>();
            var invalid = AgentPagination.Validate(args);
            if (invalid != null)
            {
                return Failure(invalid);
            }

            var active = SceneManager.GetActiveScene();
            var scenes = new List<SceneInfoPayload>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                scenes.Add(new SceneInfoPayload
                {
                    Name = scene.name,
                    Path = scene.path,
                    IsLoaded = scene.isLoaded,
                    IsDirty = scene.isDirty,
                    IsActive = scene == active,
                    RootCount = scene.isLoaded ? scene.rootCount : 0,
                });
            }

            var items = AgentPagination.Slice(scenes, args, out var page);
            return Success(items, page);
        }

        // ----- unity.object.inspect -----

        public sealed class ObjectInspectParams
        {
            public AgentObjectRef? Ref { get; set; }
        }

        public sealed class ObjectInspectPayload
        {
            public AgentObjectRef? Ref { get; set; }
            public bool? ActiveSelf { get; set; }
            public bool? ActiveInHierarchy { get; set; }
            public string? Tag { get; set; }
            public int? Layer { get; set; }
            public int? ChildCount { get; set; }
            public List<string>? Components { get; set; }
        }

        private static AgentResult<object> InspectObject(AgentToolInvocation invocation)
        {
            var parameters = invocation.GetParameters<ObjectInspectParams>();
            if (parameters?.Ref == null)
            {
                return Failure(new AgentError
                {
                    Code = AgentErrorCodes.InvalidArgument,
                    Message = "Parameter 'ref' (object reference) is required.",
                    Provider = ProviderId,
                    Retryable = false,
                });
            }

            var resolved = AgentObjectRefResolver.Resolve(parameters.Ref);
            if (resolved == null)
            {
                return Failure(new AgentError
                {
                    Code = AgentErrorCodes.ObjectNotFound,
                    Message = "No object matched the given reference.",
                    Provider = ProviderId,
                    Retryable = false,
                });
            }

            var payload = new ObjectInspectPayload
            {
                Ref = AgentObjectRefResolver.Describe(resolved),
            };

            if (resolved is GameObject gameObject)
            {
                payload.ActiveSelf = gameObject.activeSelf;
                payload.ActiveInHierarchy = gameObject.activeInHierarchy;
                payload.Tag = gameObject.tag;
                payload.Layer = gameObject.layer;
                payload.ChildCount = gameObject.transform.childCount;
                var components = new List<string>();
                foreach (var component in gameObject.GetComponents<Component>())
                {
                    components.Add(component == null ? "(missing)" : component.GetType().FullName);
                }

                payload.Components = components;
            }

            return Success(payload);
        }

        // ----- unity.selection.get -----

        private static AgentResult<object> GetSelection(AgentToolInvocation invocation)
        {
            var args = invocation.GetParameters<AgentPageArgs>();
            var invalid = AgentPagination.Validate(args);
            if (invalid != null)
            {
                return Failure(invalid);
            }

            var refs = new List<AgentObjectRef>();
            foreach (var obj in Selection.objects)
            {
                if (obj != null)
                {
                    refs.Add(AgentObjectRefResolver.Describe(obj));
                }
            }

            var items = AgentPagination.Slice(refs, args, out var page);
            return Success(items, page);
        }

        // ----- unity.console.get -----

        public sealed class ConsoleGetParams
        {
            public int? Offset { get; set; }
            public int? Limit { get; set; }
            public string? Type { get; set; }
        }

        private static AgentResult<object> GetConsole(AgentToolInvocation invocation)
        {
            var parameters = invocation.GetParameters<ConsoleGetParams>();
            var args = new AgentPageArgs { Offset = parameters?.Offset, Limit = parameters?.Limit };
            var invalid = AgentPagination.Validate(args);
            if (invalid != null)
            {
                return Failure(invalid);
            }

            var entries = AgentConsoleLogCollector.Snapshot();
            if (!string.IsNullOrEmpty(parameters?.Type))
            {
                entries = entries.FindAll(e => e.Type == parameters!.Type);
            }

            var items = AgentPagination.Slice(entries, args, out var page);
            return Success(items, page);
        }

        // ----- helpers -----

        private static AgentResult<object> Success(object payload, PageInfo? page = null)
        {
            return new AgentResult<object> { Success = true, Result = payload, Page = page };
        }

        private static AgentResult<object> Failure(AgentError error)
        {
            return new AgentResult<object> { Success = false, Error = error };
        }

        private static string ResolvePackageVersion()
        {
            try
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(CoreToolProvider).Assembly);
                return info?.version ?? "unknown";
            }
            catch (System.Exception)
            {
                return "unknown";
            }
        }
    }
}
