using Microsoft.Extensions.DependencyInjection;
using UnityEngine;
using UnityNaturalMCP.Editor;

[CreateAssetMenu(fileName = "AllMCPToolsBuilder",
 menuName = "UnityNaturalMCP/All MCP Tools Builder")]
public class AllMCPToolsBuilder : McpBuilderScriptableObject
{
    public override void Build(IMcpServerBuilder builder)
    {
        // Register all MCP tools here
        builder.WithTools<GameObjectCreatorMCPTool>();
        builder.WithTools<SeaweedCreatorMCPTool>();
        builder.WithTools<SeaweedCarpetMCPTool>();
        builder.WithTools<RefreshMCPToolsMCPTool>();
        builder.WithTools<CreateSceneMCPTool>();
        builder.WithTools<PackageManagerMCPTool>();
        builder.WithTools<PackageInstallerMCPTool>();
        builder.WithTools<CustomShapeMCPTool>();
        builder.WithTools<SimpleTestMCPTool>();
        builder.WithTools<GameObjectManagerMCPTool>();
        builder.WithTools<PrefabManagerMCPTool>();
        builder.WithTools<OrganicPatchMCPTool>();
        builder.WithTools<UICreatorMCPTool>();
        builder.WithTools<ComponentAttachMCPTool>();
        builder.WithTools<ComprehensiveUICreatorMCPTool>();
        builder.WithTools<InspectorFieldSetterMCPTool>();
        builder.WithTools<ScriptableObjectMCPTool>();
        builder.WithTools<ScriptableObjectFieldSetterMCPTool>();
        builder.WithTools<PlayModeControlMCPTool>();
        builder.WithTools<HierarchyManagerMCPTool>();
        builder.WithTools<ScreenshotCaptureMCPTool>();
        builder.WithTools<FontCopyMCPTool>();
        builder.WithTools<GetChildrenMCPTool>();
        builder.WithTools<SetChildTextFontMCPTool>();
        builder.WithTools<InstantiatePrefabMCPTool>();
        builder.WithTools<ExportPackageMCPTool>();
        builder.WithTools<WebCamDeviceMCPTool>();
        builder.WithTools<BuildSettingsMCPTool>();
        builder.WithTools<TextFontMCPTool>();
        builder.WithTools<AnimatorCleanupMCPTool>();
        builder.WithTools<WaterChannelBoxMCPTool>();

        // Add new MCP tools here in the future:
        // builder.WithTools<YourNewMCPTool>();
    }
}