using System;
using System.Collections.Generic;
using NbCore;
using NbCore.Common;
using NbCore.Math;
using NbCore.Systems;

namespace NibbleEditor
{
    public class RenderLayer :ApplicationLayer
    {
        public RenderLayer(Engine e) : base(e)
        {

        }


        public override void OnFrameUpdate(ref Queue<object> data, double dt)
        {
            //Enable Animation System
            //if (RenderState.settings.renderSettings.ToggleAnimations)
            EngineRef.GetSystem<AnimationSystem>().OnFrameUpdate(dt);

            //Update systems
            EngineRef.GetSystem<SceneManagementSystem>().OnFrameUpdate(dt);
            EngineRef.GetSystem<TransformationSystem>().OnFrameUpdate(dt);
            EngineRef.GetSystem<ScriptingSystem>().OnFrameUpdate(dt);
            
            //Enable Action System
            if (RenderState.settings.viewSettings.EmulateActions)
                EngineRef.GetSystem<ActionSystem>().OnFrameUpdate(dt);

            //Post FrameUpdate Actions
            EngineRef.GetSystem<AnimationSystem>().OnPostFrameUpdate();
            
            //Camera & Light Positions
            //Update common transforms

            //Apply extra viewport rotation
            NbMatrix4 Rotx = NbMatrix4.CreateRotationX(MathUtils.radians(RenderState.rotAngles.X));
            NbMatrix4 Roty = NbMatrix4.CreateRotationY(MathUtils.radians(RenderState.rotAngles.Y));
            NbMatrix4 Rotz = NbMatrix4.CreateRotationZ(MathUtils.radians(RenderState.rotAngles.Z));
            RenderState.rotMat = Rotz * Rotx * Roty;
            //RenderState.rotMat = Matrix4.Identity;
        }

        public override void OnRenderFrameUpdate(ref Queue<object> data, double dt)
        {
            //Per Frame System Updates
            EngineRef.GetSystem<TransformationSystem>().OnRenderUpdate(dt);
            EngineRef.GetSystem<AnimationSystem>().OnRenderUpdate(dt);
            EngineRef.GetSystem<SceneManagementSystem>().OnRenderUpdate(dt);

            //Rendering
            EngineRef.GetSystem<RenderingSystem>().OnRenderUpdate(dt);
        }

    }
}
