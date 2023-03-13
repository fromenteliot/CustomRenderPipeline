using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    private ScriptableRenderContext _context;
    private Camera _camera;

    private const string BufferName = "Render Camera";
    private readonly CommandBuffer _buffer = new CommandBuffer {name = BufferName};

    private CullingResults _cullResults;
    
    private static readonly ShaderTagId UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        _context = context;
        _camera = camera;

        if (!Cull()) return;
        
        Setup();
        DrawUnsupportedShaders();
        DrawVisibleGeometry();
        DrawGizmos();
        Submit();
    }

    private void Setup()
    {
        _context.SetupCameraProperties(_camera);
        _buffer.ClearRenderTarget(true, true, Color.clear);
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();
    }

    private void DrawVisibleGeometry ()
    {
        SortingSettings sortingSettings = new SortingSettings(_camera) {criteria = SortingCriteria.CommonOpaque};
        DrawingSettings drawingSettings = new DrawingSettings(UnlitShaderTagId, sortingSettings);
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque); // Draw opaques first
        
        _context.DrawRenderers(_cullResults, ref drawingSettings, ref filteringSettings);
        
        _context.DrawSkybox(_camera); // Then skybox
        
        sortingSettings.criteria = SortingCriteria.CommonTransparent; // then transparents
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        _context.DrawRenderers(_cullResults, ref drawingSettings, ref filteringSettings);
    }
    
    private void Submit()
    {
        _buffer.EndSample(BufferName);
        ExecuteBuffer();
        _context.Submit();
    }

    private void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
    
    bool Cull()
    {
        if (_camera.TryGetCullingParameters(out ScriptableCullingParameters parameters))
        {
            _cullResults = _context.Cull(ref parameters);
            return true;
        }
        return false;
    }
}