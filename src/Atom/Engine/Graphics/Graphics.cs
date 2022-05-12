namespace Atom.Engine;

public static class Graphics
{
    public static vk.RenderPass MainRenderPass { get; set; }

    public static uint MainSubpass { get; set; } = 0;

    public static uint MaxFramesCount { get; set; } = 3; // enable triple buffering by default

    public static uint FrameIndex { get; set; } = 0;

    public static bool IsRenderReady { get; private set; } = false;



    private static ManualResetEvent _renderReadyEvent = new(initialState: false);

    public static void SetRenderReady()
    {
        IsRenderReady = true;
        _renderReadyEvent.Set();
    } 

    public static void WaitRenderReady() => _renderReadyEvent.WaitOne();
}