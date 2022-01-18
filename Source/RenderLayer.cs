using System;
using System.Collections.Generic;
using NbCore;
using NbCore.Common;
using NbCore.Math;

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
            EngineRef.animationSys.OnFrameUpdate(dt);

            //Update systems
            EngineRef.sceneMgmtSys.OnFrameUpdate(dt);
            EngineRef.transformSys.OnFrameUpdate(dt);
            
            //Reset Stats
            RenderStats.occludedNum = 0;

            //Enable Action System
            if (RenderState.settings.viewSettings.EmulateActions)
                EngineRef.actionSys.OnFrameUpdate(dt);
            
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
            EngineRef.transformSys.OnRenderUpdate(dt);
            EngineRef.animationSys.OnRenderUpdate(dt);
            EngineRef.sceneMgmtSys.OnRenderUpdate(dt);
            EngineRef.renderSys.OnRenderUpdate(dt);
        }

    }
}
