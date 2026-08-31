using System.Collections.Generic;
using com.amari_noa.unity_agent_framework.core.editor;
using com.amari_noa.unity_agent_framework.core.editor.tools;
using com.amari_noa.unity_agent_framework.sdk;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class CoreToolsTest
    {
        private AgentToolRegistry _registry;
        private Object[] _savedSelection;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _registry = new AgentToolRegistry();
            new CoreToolProvider().RegisterTools(_registry);
            _savedSelection = Selection.objects;
        }

        [TearDown]
        public void TearDown()
        {
            Selection.objects = _savedSelection;
            for (var i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null)
                {
                    Object.DestroyImmediate(_spawned[i]);
                }
            }

            _spawned.Clear();
        }

        private GameObject Spawn(string name)
        {
            var gameObject = new GameObject(name);
            _spawned.Add(gameObject);
            return gameObject;
        }

        private AgentResult<object> Invoke(string toolId, string parametersJson = null)
        {
            var tool = _registry.Find(toolId);
            Assert.That(tool, Is.Not.Null, $"tool {toolId} must be registered");
            return tool.Handler(new AgentToolInvocation(toolId, parametersJson, false, false));
        }

        [Test]
        public void RegistersFiveReadToolsAndTheCoreProvider()
        {
            Assert.That(_registry.ListDescriptors().Count, Is.EqualTo(5));
            var providers = _registry.ListProviders();
            Assert.That(providers.Count, Is.EqualTo(1));
            Assert.That(providers[0].Id, Is.EqualTo("core"));
            foreach (var descriptor in _registry.ListDescriptors())
            {
                Assert.That(descriptor.Mutation, Is.EqualTo(AgentToolMutation.None), descriptor.Id);
                Assert.That(descriptor.InputSchemaJson, Is.Not.Null.And.Not.Empty, descriptor.Id);
            }
        }

        [Test]
        public void ProjectInfoReturnsCurrentProjectValues()
        {
            var result = Invoke("unity.project.info");

            Assert.That(result.Success, Is.True);
            var payload = (CoreToolProvider.ProjectInfoPayload)result.Result;
            Assert.That(payload.UnityVersion, Is.EqualTo(Application.unityVersion));
            Assert.That(payload.ProjectPath, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void SceneListReturnsLoadedScenesWithPageInfo()
        {
            var result = Invoke("unity.scene.list");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Page, Is.Not.Null);
            var scenes = (List<CoreToolProvider.SceneInfoPayload>)result.Result;
            Assert.That(scenes.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.Page.Total, Is.EqualTo(scenes.Count + result.Page.Offset).Or.GreaterThanOrEqualTo(scenes.Count));
        }

        [Test]
        public void SceneListRejectsExcessiveLimit()
        {
            var result = Invoke("unity.scene.list", "{\"limit\":1001}");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(AgentErrorCodes.InvalidArgument));
        }

        [Test]
        public void SelectionGetDescribesSelectedObjects()
        {
            var gameObject = Spawn("UafSelectionTarget");
            Selection.objects = new Object[] { gameObject };

            var result = Invoke("unity.selection.get");

            Assert.That(result.Success, Is.True);
            var refs = (List<AgentObjectRef>)result.Result;
            Assert.That(refs.Count, Is.EqualTo(1));
            Assert.That(refs[0].Name, Is.EqualTo("UafSelectionTarget"));
            Assert.That(refs[0].HierarchyPath, Is.EqualTo("/UafSelectionTarget"));
            Assert.That(refs[0].CanonicalUri, Does.StartWith("unity://scene/"));
        }

        [Test]
        public void ObjectInspectResolvesByInstanceId()
        {
            var parent = Spawn("UafInspectParent");
            Spawn("UafInspectChild").transform.SetParent(parent.transform);

            var result = Invoke(
                "unity.object.inspect",
                "{\"ref\":{\"instanceId\":" + parent.GetInstanceID() + "}}");

            Assert.That(result.Success, Is.True);
            var payload = (CoreToolProvider.ObjectInspectPayload)result.Result;
            Assert.That(payload.Ref.Name, Is.EqualTo("UafInspectParent"));
            Assert.That(payload.ChildCount, Is.EqualTo(1));
            Assert.That(payload.Components, Does.Contain("UnityEngine.Transform"));
        }

        [Test]
        public void ObjectInspectResolvesByHierarchyPath()
        {
            var parent = Spawn("UafPathParent");
            var child = Spawn("UafPathChild");
            child.transform.SetParent(parent.transform);

            var result = Invoke(
                "unity.object.inspect",
                "{\"ref\":{\"hierarchyPath\":\"/UafPathParent/UafPathChild\"}}");

            Assert.That(result.Success, Is.True);
            var payload = (CoreToolProvider.ObjectInspectPayload)result.Result;
            Assert.That(payload.Ref.Name, Is.EqualTo("UafPathChild"));
        }

        [Test]
        public void ObjectInspectReportsMissingAndUnresolvableReferences()
        {
            var missing = Invoke("unity.object.inspect", "{}");
            Assert.That(missing.Error.Code, Is.EqualTo(AgentErrorCodes.InvalidArgument));

            var unresolved = Invoke(
                "unity.object.inspect",
                "{\"ref\":{\"hierarchyPath\":\"/UafNoSuchObject_1b7c\"}}");
            Assert.That(unresolved.Error.Code, Is.EqualTo(AgentErrorCodes.ObjectNotFound));
        }

        [Test]
        public void ConsoleGetReturnsCollectedEntriesWithTypeFilter()
        {
            AgentConsoleLogCollector.Initialize();
            AgentConsoleLogCollector.ClearForTests();
            Debug.Log("UafConsoleMarkerLog");
            Debug.LogWarning("UafConsoleMarkerWarning");
            LogAssertIgnoreWarning();

            var all = Invoke("unity.console.get");
            Assert.That(all.Success, Is.True);
            var entries = (List<AgentConsoleEntry>)all.Result;
            Assert.That(entries.Exists(e => e.Message == "UafConsoleMarkerLog" && e.Type == "log"), Is.True);

            var warningsOnly = Invoke("unity.console.get", "{\"type\":\"warning\"}");
            var warnings = (List<AgentConsoleEntry>)warningsOnly.Result;
            Assert.That(warnings.TrueForAll(e => e.Type == "warning"), Is.True);
            Assert.That(warnings.Exists(e => e.Message == "UafConsoleMarkerWarning"), Is.True);
        }

        private static void LogAssertIgnoreWarning()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, "UafConsoleMarkerWarning");
        }
    }
}
