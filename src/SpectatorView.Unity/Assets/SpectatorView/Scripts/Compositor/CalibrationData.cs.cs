﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Used for loading/applying camera intrinsics and camera extrinsics obtained through Spectator View's default calibration process for a
    /// camera attached to a head-mounted display.
    /// </summary>
    public abstract class CalibrationData : ICalibrationData
    {
        private CalculatedCameraIntrinsics cameraIntrinsics;
        private CalculatedCameraExtrinsics cameraExtrinsics;

        protected CalibrationData(string cameraIntrinsicsPath, string cameraExtrinsicsPath)
        {
            if (File.Exists(cameraIntrinsicsPath) &&
                File.Exists(cameraExtrinsicsPath))
            {
                cameraIntrinsics = CalibrationDataHelper.LoadCameraIntrinsics(cameraIntrinsicsPath);
                cameraExtrinsics = CalibrationDataHelper.LoadCameraExtrinsics(cameraExtrinsicsPath);

                if (cameraIntrinsics == null ||
                    cameraExtrinsics == null)
                {
                    Debug.LogError($"Failed to load cameara intrinsics/extrinscs: {cameraIntrinsicsPath}, {cameraExtrinsicsPath}");
                }
            }
            else
            {
                Debug.LogError($"Invalid paths provided for camera intrinsics/extrinsics: {cameraIntrinsicsPath}, {cameraExtrinsicsPath}");
            }
        }

        protected CalibrationData(CalculatedCameraIntrinsics intrinsics, CalculatedCameraExtrinsics extrinsics)
        {
            cameraIntrinsics = intrinsics;
            cameraExtrinsics = extrinsics;
        }

        /// <inheritdoc />
        public virtual void SetUnityCameraExtrinstics(Transform cameraTransform)
        {
            Matrix4x4 headFromCamera = cameraExtrinsics.ViewFromWorld;
            cameraTransform.transform.localPosition = cameraExtrinsics.ViewFromWorld.GetColumn(3);
            cameraTransform.transform.localRotation = Quaternion.LookRotation(cameraExtrinsics.ViewFromWorld.GetColumn(2), cameraExtrinsics.ViewFromWorld.GetColumn(1));
        }

        /// <inheritdoc />
        public virtual void SetUnityCameraIntrinsics(Camera camera)
        {
            // D3D projection matrix (ProjectionMatrixToUnity accounts for it)
            Matrix4x4 projection = Matrix4x4.zero;
            projection[0, 0] = 2;
            projection[1, 1] = -2;
            projection[2, 0] = -1;
            projection[2, 1] = 1;
            projection[2, 2] = (camera.farClipPlane / (camera.farClipPlane - camera.nearClipPlane));
            projection[2, 3] = 1;
            projection[3, 2] = ((-camera.farClipPlane * camera.nearClipPlane) / (camera.farClipPlane - camera.nearClipPlane));

            float PX = cameraIntrinsics.PrincipalPoint.x / cameraIntrinsics.ImageWidth;
            float PY = cameraIntrinsics.PrincipalPoint.y / cameraIntrinsics.ImageHeight;
            float FX = cameraIntrinsics.FocalLength.x / cameraIntrinsics.ImageWidth;
            float FY = cameraIntrinsics.FocalLength.y / cameraIntrinsics.ImageHeight;

            // Affine matrix from calibration values
            Matrix4x4 affine = Matrix4x4.identity;
            affine[0, 0] = FX;
            affine[1, 1] = FY;
            affine[2, 0] = PX;
            affine[2, 1] = PY;

            // We insert a YZFlip on the front end to compensate for the YZFlip exiting the View matrix layer.
            var yzflip = Matrix4x4.identity;
            yzflip[1, 1] = -1;
            yzflip[2, 2] = -1;

            camera.projectionMatrix = ProjectionMatrixToUnity((yzflip * affine * projection).transpose);
        }

        private static Matrix4x4 ProjectionMatrixToUnity(Matrix4x4 m)
        {
            // to convert from a WinRT/D3D projection matrix to a Unity projection matrix (openGL-style), need to:
            //      - first convert the matrix generally - transpose since Unity uses column vectors and WinRT/D3D uses row vectors
            //      - recalculate the near/far plane components using the openGL convention
            // m23 = B, m22 = -A, 
            // D3D:     A = f/(f-n),       B = -fn(f-n)     ==> n = -B/A, f = -B/(A-1)  
            // OpenGL:  A' = -(f+n)/(f-n), B' = -2fn/(f-n)  ==> A' = 1-2A, B' = 2B  ==> A' = 1+2*m22, B' = 2*m23

            m.m22 = 1.0f + 2.0f * m.m22;
            m.m23 *= 2.0f;
            return m;
        }
    }
}