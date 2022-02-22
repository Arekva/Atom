using System.Collections.Concurrent;
using Silk.NET.Maths;

namespace Atom.Engine;

public static class Draw
{
    private static vk.Device _device;
    

    private static Dictionary<uint, List<IDrawer>>[] _drawersAdditionList =  
        new Dictionary<uint, List<IDrawer>>[Graphics.MaxFramesCount];
    
    private static Dictionary<uint, List<IDrawer>>[] _drawersDeletionList =  
        new Dictionary<uint, List<IDrawer>>[Graphics.MaxFramesCount];
    
    private static Dictionary<uint, List<IDrawer>>[] _drawersCurrentList =  
        new Dictionary<uint, List<IDrawer>>[Graphics.MaxFramesCount];


    public static void Initialize(vk.Device? device = null)
    {
        _device = device ?? VK.Device;
        
        const int camCount = (int) CameraData.MaxCameraCount;
        for (int i = 0; i < _drawersAdditionList.Length; i++)
        {
            _drawersAdditionList[i] = new Dictionary<uint, List<IDrawer>>(capacity: camCount);
            _drawersDeletionList[i] = new Dictionary<uint, List<IDrawer>>(capacity: camCount);
            _drawersCurrentList [i] = new Dictionary<uint, List<IDrawer>>(capacity: camCount);
            
            for (uint j = 0; j < camCount; j++)
            {
                _drawersAdditionList[i].Add(j, new List<IDrawer>());
                _drawersDeletionList[i].Add(j, new List<IDrawer>());
                _drawersCurrentList [i].Add(j, new List<IDrawer>());
            }
        }
    }
    
    public static void Cleanup()
    {
        
    }
    
    
    public static void AssignDrawer(IDrawer drawer, uint cameraIndex)
    {
        for (int i = 0; i < Graphics.MaxFramesCount; i++)
        {
            _drawersAdditionList[i][cameraIndex].Add(drawer);
            _drawersDeletionList[i][cameraIndex].Remove(drawer);
        }
    }
    public static void UnassignDrawer(IDrawer drawer, uint cameraIndex)
    {
        for (int i = 0; i < Graphics.MaxFramesCount; i++)
        {
            _drawersAdditionList[i][cameraIndex].Remove(drawer);
            _drawersDeletionList[i][cameraIndex].Add(drawer);
        }
    }

    public static bool HasUpdates(uint frameIndex, uint cameraIndex)
    {
        return 
            _drawersAdditionList[frameIndex][cameraIndex].Any() || 
            _drawersDeletionList[frameIndex][cameraIndex].Any() ;
    }

    public static void UpdateFrame(SlimCommandBuffer cmd, Vector2D<uint> extent, uint cameraIndex, uint frameIndex)
    {
        foreach (IDrawer drawer in _drawersDeletionList[frameIndex][cameraIndex]) // remove
        {
            _drawersCurrentList[frameIndex][cameraIndex].Remove(drawer);
        }
        _drawersDeletionList[frameIndex][cameraIndex].Clear();
        
        foreach (IDrawer drawer in _drawersAdditionList[frameIndex][cameraIndex]) // add
        {
            _drawersCurrentList[frameIndex][cameraIndex].Add(drawer);
        }
        _drawersAdditionList[frameIndex][cameraIndex].Clear();
        
        foreach (IDrawer drawer in _drawersCurrentList[frameIndex][cameraIndex]) // perform
        {
            drawer.CmdDraw(cmd, extent, cameraIndex, frameIndex);
        }
    }
}