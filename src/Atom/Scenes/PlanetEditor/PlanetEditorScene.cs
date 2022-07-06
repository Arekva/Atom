using Atom.Engine;
using Silk.NET.Maths;

namespace Atom.Game.PlanetEditor;

public class PlanetEditorScene : AtomObject, IScene
{
    private Camera _uiCamera;

    public PlanetEditorScene()
    {
        _uiCamera = Camera.UserInterface = new Camera(
            identifier: "editor_ui_camera", 
            projectionMode: Projection.Orthographic
        );
        
        //Mouse.CursorMode = CursorMode.Normal;


        //_planetCamera = new EditorPlanetCamera();
        
        
        //_sky = new SkySphere(color: new Vector4D<f64>(135/255.0D, 206/255.0D, 235/255.0D, 20000.0D));

        
        
        MakeReady();
    }

    protected internal override void Render()
    {
        base.Render();

        if (Keyboard.IsPressing(Key.Escape))
        {
            Mouse.GameFocus = false;
        }
    }

    public override void Delete()
    {
        base.Delete();
        
        _uiCamera.Delete();
    }
}