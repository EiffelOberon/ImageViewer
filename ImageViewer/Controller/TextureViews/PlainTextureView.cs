﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageFramework.DirectX;
using ImageViewer.Controller.TextureViews.Shader;
using ImageViewer.Models;
using SharpDX;
using SharpDX.Direct3D11;
using Point = System.Drawing.Point;

namespace ImageViewer.Controller.TextureViews
{
    public class PlainTextureView : ITextureView
    {
        protected readonly ModelsEx models;
        private readonly TextureViewData data;
        private Vector3 translation = Vector3.Zero;
        private readonly SingleViewShader shader;

        public PlainTextureView(ModelsEx models, TextureViewData data)
        {
            this.models = models;
            this.data = data;
            shader = new SingleViewShader();
        }

        public void Dispose()
        {
            shader.Dispose();
        }

        public virtual void Draw(TextureArray2D texture)
        {
            throw new NotImplementedException();
        }

        public void OnScroll(float amount, Vector2 mouse)
        {
            // modify zoom
            var step = amount < 0.0f ? 1.0f / 1.001f : 1.001f;
            var value = (float)Math.Pow(step, Math.Abs(amount));

            var oldZoom = models.Display.Zoom;


            models.Display.Zoom = models.Display.Zoom * value;

            // do this because zoom is clamped and may not have changed at all
            value = models.Display.Zoom / oldZoom;
            // modify translation as well
            translation.X *= value;
            translation.Y *= value;
        }

        public void OnDrag(Vector2 diff)
        {
            // window to client
            translation.X += diff.X * 2.0f / models.Images.GetWidth(0);
            translation.Y -= diff.Y * 2.0f / models.Images.GetHeight(0);
        }

        public virtual Point GetTexelPosition(Vector2 mouse)
        {
            throw new NotImplementedException();
        }

        private Matrix GetTransform()
        {
            return Matrix.Scaling(models.Display.Zoom, models.Display.Zoom, 1.0f) *
                   Matrix.Translation(translation) * models.Display.ImageAspectRatio;
        }

        /// <summary>
        /// transforms mouse coordinates into directX space
        /// </summary>
        /// <param name="mouse">canonical mouse coordinates</param>
        /// <returns>vector with correct x and y coordinates</returns>
        protected Vector2 GetDirectXMouseCoordinates(Vector2 mouse)
        {
            // Matrix Coordinate system is reversed (left handed)
            var vec = new Vector4((float)mouse.X, (float)mouse.Y, 0.0f, 1.0f);
            var trans = (GetOrientation() * GetTransform());

            Vector4.Transform(ref vec, ref trans, out var res);

            return new Vector2(res.X, res.Y);
        }

        private Matrix GetOrientation()
        {
            return Matrix.Scaling(1.0f, -1.0f, 1.0f);
        }

        protected void DrawLayer(Matrix offset, int layer, ShaderResourceView texture)
        {
            var dev = ImageFramework.DirectX.Device.Get();
            var finalTransform = offset * GetTransform();

            // draw the checkers background
            data.Checkers.Run(data.Buffer, finalTransform);

            // blend over the final image
            dev.OutputMerger.BlendState = data.AlphaBlendState;

            shader.Run(data.Buffer, finalTransform, data.GetCrop(models, layer), models.Display.Multiplier, texture, data.GetSampler(models.Display.LinearInterpolation));

            dev.OutputMerger.BlendState = data.DefaultBlendState;
        }
    }
}
