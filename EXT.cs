using Silk.NET.OpenXR;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenXRExtensions
{
    public class HandTracking: IDisposable
    {

        public unsafe void Initialize(XR xr, Session session, Instance instance, ulong system_id)
        {
            SystemHandTrackingPropertiesEXT hand_tracking_props = new SystemHandTrackingPropertiesEXT()
            {
                Type = StructureType.TypeSystemHandTrackingPropertiesExt,
                Next = null,
                SupportsHandTracking = 0,
            };
            SystemProperties system_props = new SystemProperties()
            {
                Type = StructureType.TypeSystemProperties,
                Next = &hand_tracking_props,
            };
            CheckResult(xr.GetSystemProperties(instance, system_id, &system_props), "GetSystemProperties");
            System.Console.WriteLine("Supports hand tracking: " + hand_tracking_props.SupportsHandTracking);

            Silk.NET.Core.PfnVoidFunction xrCreateHandTrackerEXT = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(xr.GetInstanceProcAddr(instance, "xrCreateHandTrackerEXT", ref xrCreateHandTrackerEXT), "GetinstanceProcAddr::xrCreateHandTrackerEXT");
            Delegate hand_tracker = Marshal.GetDelegateForFunctionPointer((IntPtr)xrCreateHandTrackerEXT.Handle, typeof(pfnxrCreateHandTrackerEXT));

            HandTrackerCreateInfoEXT left_hand_tracker_create_info = new HandTrackerCreateInfoEXT()
            {
                Type = StructureType.TypeHandTrackerCreateInfoExt,
                Next = null,
                Hand = HandEXT.HandLeftExt,
                HandJointSet = HandJointSetEXT.HandJointSetDefaultExt,
            };
            HandTrackerEXT left_hand_tracker = new HandTrackerEXT();
            var result = hand_tracker.DynamicInvoke(session, new IntPtr(&left_hand_tracker_create_info), new IntPtr(&left_hand_tracker));
            CheckResult((Result)result, "xrCreateHandTrackerEXT");
            leftHandTracker = left_hand_tracker;

            HandTrackerCreateInfoEXT right_hand_tracker_create_info = new HandTrackerCreateInfoEXT()
            {
                Type = StructureType.TypeHandTrackerCreateInfoExt,
                Next = null,
                Hand = HandEXT.HandRightExt,
                HandJointSet = HandJointSetEXT.HandJointSetDefaultExt,
            };
            HandTrackerEXT right_hand_tracker = new HandTrackerEXT();
            hand_tracker.DynamicInvoke(session, new IntPtr(&right_hand_tracker_create_info), new IntPtr(&right_hand_tracker));
            CheckResult((Result)result, "xrCreateHandTrackerEXT");
            rightHandTracker = right_hand_tracker;

            Silk.NET.Core.PfnVoidFunction xrLocateHandJointsEXT = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(xr.GetInstanceProcAddr(instance, "xrLocateHandJointsEXT", ref xrLocateHandJointsEXT), "GetinstanceProcAddr::xrLocateHandJointsEXT");
            locateHandJointsEXT = Marshal.GetDelegateForFunctionPointer((IntPtr)xrLocateHandJointsEXT.Handle, typeof(pfnxrLocateHandJointsEXT));

            Silk.NET.Core.PfnVoidFunction xrDestroyHandTrackerEXT = new Silk.NET.Core.PfnVoidFunction();
            CheckResult(xr.GetInstanceProcAddr(instance, "xrDestroyHandTrackerEXT", ref xrDestroyHandTrackerEXT), "GetinstanceProcAddr::xrDestroyHandTrackerEXT");
            destroyHandTrackerEXT = Marshal.GetDelegateForFunctionPointer((IntPtr)xrDestroyHandTrackerEXT.Handle, typeof(pfnxrDestroyHandTrackerEXT));
        }

        public unsafe void Update(Space playSpace, FrameState frameState)
        {
            HandJointsLocateInfoEXT hand_joints_locate_info = new HandJointsLocateInfoEXT()
            {
                Type = StructureType.TypeHandJointsLocateInfoExt,
                Next = null,
                BaseSpace = playSpace,
                Time = frameState.PredictedDisplayTime,
            };
            fixed (HandJointLocationEXT* joint_locations_ptr = leftHandJointLocations)
            {
                HandJointLocationsEXT hand_joint_locations = new HandJointLocationsEXT()
                {
                    Type = StructureType.TypeHandJointLocationsExt,
                    Next = null,
                    IsActive = 0,
                    JointCount = 26, //XR_HAND_JOINT_COUNT_EXT
                    JointLocations = joint_locations_ptr,
                };

                var result = locateHandJointsEXT.DynamicInvoke(leftHandTracker, new IntPtr(&hand_joints_locate_info), new IntPtr(&hand_joint_locations));
                CheckResult((Result)result, "xrLocateHandJointsEXT");

                //if (hand_joint_locations.IsActive == 1)
                //{
                //    System.Console.WriteLine("Left hand joint 0 position: " + leftHandJointLocations[0].Pose.Position.X + ", " + leftHandJointLocations[0].Pose.Position.Y + ", " + leftHandJointLocations[0].Pose.Position.Z);
                //}
            }
            fixed (HandJointLocationEXT* joint_locations_ptr = rightHandJointLocations)
            {
                HandJointLocationsEXT hand_joint_locations = new HandJointLocationsEXT()
                {
                    Type = StructureType.TypeHandJointLocationsExt,
                    Next = null,
                    IsActive = 0,
                    JointCount = 26, //XR_HAND_JOINT_COUNT_EXT
                    JointLocations = joint_locations_ptr,
                };

                var result = locateHandJointsEXT.DynamicInvoke(rightHandTracker, new IntPtr(&hand_joints_locate_info), new IntPtr(&hand_joint_locations));
                CheckResult((Result)result, "xrLocateHandJointsEXT");

                //if (hand_joint_locations.IsActive == 1)
                //{
                //    System.Console.WriteLine("Right hand joint 0 position: " + rightHandJointLocations[0].Pose.Position.X + ", " + rightHandJointLocations[0].Pose.Position.Y + ", " + rightHandJointLocations[0].Pose.Position.Z);
                //}
            }
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
            destroyHandTrackerEXT.DynamicInvoke(leftHandTracker);
            destroyHandTrackerEXT.DynamicInvoke(rightHandTracker);
        }

        private unsafe delegate Result pfnxrCreateHandTrackerEXT(Session session, HandTrackerCreateInfoEXT* createInfo, HandTrackerEXT* handTracker);
        private unsafe delegate Result pfnxrLocateHandJointsEXT(HandTrackerEXT handTracker, HandJointsLocateInfoEXT* locateInfo, HandJointLocationsEXT* locations);
        private unsafe delegate Result pfnxrDestroyHandTrackerEXT(HandTrackerEXT handTracker);

        private HandTrackerEXT leftHandTracker = new HandTrackerEXT();
        private HandTrackerEXT rightHandTracker = new HandTrackerEXT();

        public HandJointLocationEXT[] leftHandJointLocations = new HandJointLocationEXT[26];
        public HandJointLocationEXT[] rightHandJointLocations = new HandJointLocationEXT[26];

        private Delegate locateHandJointsEXT;
        private Delegate destroyHandTrackerEXT;
    }
}
