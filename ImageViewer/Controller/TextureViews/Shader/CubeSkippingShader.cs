﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageFramework.DirectX;
using ImageFramework.Model.Shader;
using ImageFramework.Utility;
using SharpDX.DXGI;
using Device = ImageFramework.DirectX.Device;

namespace ImageViewer.Controller.TextureViews.Shader
{
    public class CubeSkippingShader : IDisposable
    {
        private readonly ImageFramework.DirectX.Shader compute;
        private TransformShader initTexShader;
        private readonly Size3 workgroupSize;

        public CubeSkippingShader()
        {
            workgroupSize = new Size3(
                ShaderBuilder.Builder3D.LocalSizeX, 
                ShaderBuilder.Builder3D.LocalSizeY,
                ShaderBuilder.Builder3D.LocalSizeZ
            );

            initTexShader = new TransformShader(
                "return value.a > 0 ? 0 : 255", "float4", "uint");

            compute = new ImageFramework.DirectX.Shader(ImageFramework.DirectX.Shader.Type.Compute, GetSource(), "CubeSkippingShader");
        }

        private struct BufferData
        {
            public Size3 Size;
            public int Iteration;
        }

        public void Run(ITexture src, ITexture dst, ITexture tmpTex, LayerMipmapSlice lm, UploadBuffer upload)
        {
            var size = src.Size.GetMip(lm.Mipmap);
            var dev = Device.Get();
            // debugging
            var originalDst = dst;

            initTexShader.Run(src, dst, lm, upload);
            initTexShader.Run(src, tmpTex, lm, upload);

            ImageFramework.DirectX.Query.SyncQuery syncQuery = new ImageFramework.DirectX.Query.SyncQuery();
            //var watch = new Stopwatch();
            //watch.Start();
            for (int i = 0; i < 254; ++i)
            {
                // bind textures
                dev.Compute.SetShaderResource(0, dst.GetSrView(lm));
                dev.Compute.SetUnorderedAccessView(0, tmpTex.GetUaView(lm.Mipmap));

                dev.Compute.Set(compute.Compute);
                upload.SetData(new BufferData
                {
                    Size = size,
                    Iteration = i
                });
                dev.Compute.SetConstantBuffer(0, upload.Handle);

                // execute
                dev.Dispatch(
                    Utility.DivideRoundUp(size.X, workgroupSize.X),
                    Utility.DivideRoundUp(size.Y, workgroupSize.Y),
                    Utility.DivideRoundUp(size.Z, workgroupSize.Z)
                );

                // unbind texture
                dev.Compute.SetShaderResource(0, null);
                dev.Compute.SetUnorderedAccessView(0, null);

                // swap textures
                var tmp = tmpTex;
                tmpTex = dst;
                dst = tmp;

#if DEBUG
                if (i % 16 == 0)
                {
                    syncQuery.Set();
                    syncQuery.WaitForGpu();
                    Console.WriteLine("Iteration: " + i);
                }
#endif
            }

            /*syncQuery.Set();
            syncQuery.WaitForGpu();
            watch.Stop();

            Console.WriteLine($"Time: {watch.ElapsedMilliseconds}ms");
            */
            dev.Compute.Set(null);

            Debug.Assert(ReferenceEquals(originalDst, dst));
            syncQuery?.Dispose();
        }

        public void Dispose()
        {
            initTexShader?.Dispose();
        }

        private string GetSource()
        {
            return $@"
cbuffer DirBuffer : register(b0){{
    int3 size;
    uint iteration;
}};

Texture3D<uint> in_tex : register(t0);
RWTexture3D<uint> out_tex : register(u0);

[numthreads({workgroupSize.X},{workgroupSize.Y},{workgroupSize.Z})]
void main(int3 id : SV_DispatchThreadID) {{
    if(any(id >= size)) return; // outside
    if(in_tex[id] == 0) return; // remains zero    
    if(in_tex[id] < iteration) return; // stays the same

    uint minVal = 255;
    
    [unroll] for(uint i = 0; i < 3; ++i){{
        int3 offset = 0;
        offset[i] = 1;
        [flatten] if(id[i] > 0) minVal = min(minVal, in_tex[id - offset]);
        [flatten] if(id[i] + 1 < size[i]) minVal = min(minVal, in_tex[id + offset]);
    }}

    minVal = min(minVal + 1, 255);
    //if(minVal != 255)
        out_tex[id] = minVal;
}}
";
        }
    }
}
