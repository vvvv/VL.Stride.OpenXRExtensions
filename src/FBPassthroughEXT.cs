using Silk.NET.OpenXR;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenXRExtensions
{
    public class FBPassthrough : IDisposable
    {

        public unsafe void Initialize(XR xr, Session session, Instance instance, ulong system_id)
        {
            Silk.NET.Core.PfnVoidFunction xrCreatePassthroughFB = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(xr.GetInstanceProcAddr(instance, "xrCreatePassthroughFB", ref xrCreatePassthroughFB), "GetinstanceProcAddr::xrCreatePassthroughFB");
            Delegate createPassthroughFB = Marshal.GetDelegateForFunctionPointer((IntPtr)xrCreatePassthroughFB.Handle, typeof(pfnxrCreatePassthroughFB));

            Silk.NET.Core.PfnVoidFunction xrDestroyPassthroughFB = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(xr.GetInstanceProcAddr(instance, "xrDestroyPassthroughFB", ref xrDestroyPassthroughFB), "GetinstanceProcAddr::xrDestroyPassthroughFB");
            destroyPassthroughFB = Marshal.GetDelegateForFunctionPointer((IntPtr)xrDestroyPassthroughFB.Handle, typeof(pfnxrDestroyPassthroughFB));

            Silk.NET.Core.PfnVoidFunction xrPassthroughStartFB = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(xr.GetInstanceProcAddr(instance, "xrPassthroughStartFB", ref xrPassthroughStartFB), "GetinstanceProcAddr::xrPassthroughStartFB");
            passthroughStartFB = Marshal.GetDelegateForFunctionPointer((IntPtr)xrPassthroughStartFB.Handle, typeof(pfnxrPassthroughStartFB));

            Silk.NET.Core.PfnVoidFunction xrPassthroughPauseFB = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(xr.GetInstanceProcAddr(instance, "xrPassthroughPauseFB", ref xrPassthroughPauseFB), "GetinstanceProcAddr::xrPassthroughPauseFB");
            passthroughPauseFB = Marshal.GetDelegateForFunctionPointer((IntPtr)xrPassthroughPauseFB.Handle, typeof(pfnxrPassthroughPauseFB));

            Silk.NET.Core.PfnVoidFunction xrCreatePassthroughLayerFB = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(xr.GetInstanceProcAddr(instance, "xrCreatePassthroughLayerFB", ref xrCreatePassthroughLayerFB), "GetinstanceProcAddr::xrCreatePassthroughLayerFB");
            Delegate createPassthroughLayerFB = Marshal.GetDelegateForFunctionPointer((IntPtr)xrCreatePassthroughLayerFB.Handle, typeof(pfnxrCreatePassthroughLayerFB));

            Silk.NET.Core.PfnVoidFunction xrDestroyPassthroughLayerFB = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(xr.GetInstanceProcAddr(instance, "xrDestroyPassthroughLayerFB", ref xrDestroyPassthroughLayerFB), "GetinstanceProcAddr::xrDestroyPassthroughLayerFB");
            destroyPassthroughLayerFB = Marshal.GetDelegateForFunctionPointer((IntPtr)xrDestroyPassthroughLayerFB.Handle, typeof(pfnxrDestroyPassthroughLayerFB));

            var passthroughCreateInfo = new PassthroughCreateInfoFB
            {
                Next = null,
                Flags = PassthroughFlagsFB.PassthroughIsRunningATCreationBitFB,
                Type = StructureType.TypePassthroughCreateInfoFB
            };

            var passthrough = new PassthroughFB();            
            var result = createPassthroughFB.DynamicInvoke(session, new IntPtr(&passthrough), new IntPtr(&passthroughCreateInfo));

            activePassthrough = passthrough;

            var passthroughLayerCreateInfo = new PassthroughLayerCreateInfoFB
            {
                Next = null,
                Flags = 0,
                Passthrough = passthrough,
                Purpose = PassthroughLayerPurposeFB.PassthroughLayerPurposeProjectedFB,
                Type = StructureType.TypePassthroughLayerCreateInfoFB
            };
            var passthroughLayer = new PassthroughLayerFB();
            createPassthroughLayerFB.DynamicInvoke(session, new IntPtr(&passthroughLayer), new IntPtr(&passthroughLayerCreateInfo));
            activeLayer = passthroughLayer;

            passthroughStartFB.DynamicInvoke(passthrough);

            activeCompositionlayer = Marshal.AllocHGlobal(sizeof(CompositionLayerPassthroughFB));
        }

        public unsafe IntPtr Update(Space playSpace, FrameState frameState)
        {
            var activeCompositionlayer = (CompositionLayerPassthroughFB*)this.activeCompositionlayer;
            activeCompositionlayer->Next = null;
            activeCompositionlayer->Flags = CompositionLayerFlags.CompositionLayerBlendTextureSourceAlphaBit;
            activeCompositionlayer->LayerHandle = activeLayer;
            activeCompositionlayer->Space = playSpace;
            return this.activeCompositionlayer;
        }

        /// <summary>
        /// A simple function which throws an exception if the given OpenXR result indicates an error has been raised.
        /// </summary>
        /// <param name="result">The OpenXR result in question.</param>
        /// <returns>
        /// The same result passed in, just in case it's meaningful and we just want to use this to filter out errors.
        /// </returns>
        /// <exception cref="Exception">An exception for the given result if it indicates an error.</exception>
        [DebuggerHidden]
        [DebuggerStepThrough]
        protected internal static Result CheckResult(Result result, string forFunction)
        {
            if ((int)result < 0)
                throw new InvalidOperationException($"OpenXR error! Make sure a OpenXR runtime is set & running (like SteamVR)\n\nCode: {result} ({result:X}) in " + forFunction + "\n\nStack Trace: " + (new StackTrace()).ToString());

            return result;
        }

        public void Dispose()
        {
            destroyPassthroughFB.DynamicInvoke(activePassthrough);
            destroyPassthroughLayerFB.DynamicInvoke(activeLayer);
            if (activeCompositionlayer != null)
                Marshal.FreeHGlobal(activeCompositionlayer);
        }

        private unsafe delegate Result pfnxrCreatePassthroughFB(Session session, PassthroughCreateInfoFB* createInfo, PassthroughFB* outPassthrough);
        private unsafe delegate Result pfnxrCreatePassthroughLayerFB(Session session, PassthroughLayerCreateInfoFB* createInfo, PassthroughLayerFB* outPassthroughLayer);
        private unsafe delegate Result pfnxrDestroyPassthroughFB(PassthroughFB* outPassthrough);
        private unsafe delegate Result pfnxrDestroyPassthroughLayerFB(PassthroughLayerFB* outPassthroughLayer);
        private unsafe delegate Result pfnxrPassthroughStartFB(PassthroughFB* outPassthrough);
        private unsafe delegate Result pfnxrPassthroughPauseFB(PassthroughFB* outPassthrough);


        bool extAvailable;
        bool enabled;
        bool enabledPassthrough;
        bool enableOnInitialize;
        bool passthroughRunning;
        PassthroughFB activePassthrough;
        PassthroughLayerFB activeLayer = new PassthroughLayerFB();

        private IntPtr activeCompositionlayer;

        private Delegate destroyPassthroughFB;
        private Delegate destroyPassthroughLayerFB;
        private Delegate passthroughStartFB;
        private Delegate passthroughPauseFB;
    }
}
