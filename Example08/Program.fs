﻿open OpenTK
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL4

type Vertex = 
    struct
        val Position : Vector4
        val UV : Vector2

        new(position : Vector4, uv : Vector2) = { Position = position; UV = uv }
    end 
    
type Window(width, height, mode, title, options) =
    inherit GameWindow(width, height, mode, title, options)

    let quad p0 p1 p2 p3 = 
        [| p0; p1; p2; p2; p1; p3 |]
        
    let points =
        [|
            // Front quad
            quad 
                <| Vertex(Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Vector2(0.0f, 0.0f))
                <| Vertex(Vector4(-1.0f,  1.0f, -1.0f, 1.0f), Vector2(0.0f, 1.0f))
                <| Vertex(Vector4( 1.0f, -1.0f, -1.0f, 1.0f), Vector2(1.0f, 0.0f))
                <| Vertex(Vector4( 1.0f,  1.0f, -1.0f, 1.0f), Vector2(1.0f, 1.0f))

            // Right quad
            quad 
                <| Vertex(Vector4(1.0f, -1.0f, -1.0f, 1.0f), Vector2(0.0f, 0.0f))
                <| Vertex(Vector4(1.0f,  1.0f, -1.0f, 1.0f), Vector2(1.0f, 0.0f))
                <| Vertex(Vector4(1.0f, -1.0f,  1.0f, 1.0f), Vector2(0.0f, 1.0f))
                <| Vertex(Vector4(1.0f,  1.0f,  1.0f, 1.0f), Vector2(1.0f, 1.0f))
                
            // Left quad
            quad 
                <| Vertex(Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Vector2(0.0f, 0.0f))
                <| Vertex(Vector4(-1.0f, -1.0f,  1.0f, 1.0f), Vector2(0.0f, 1.0f))
                <| Vertex(Vector4(-1.0f,  1.0f, -1.0f, 1.0f), Vector2(1.0f, 0.0f))
                <| Vertex(Vector4(-1.0f,  1.0f,  1.0f, 1.0f), Vector2(1.0f, 1.0f))

            // Back quad
            quad 
                <| Vertex(Vector4(-1.0f, -1.0f, 1.0f, 1.0f), Vector2(0.0f, 0.0f))
                <| Vertex(Vector4( 1.0f, -1.0f, 1.0f, 1.0f), Vector2(1.0f, 0.0f))
                <| Vertex(Vector4(-1.0f,  1.0f, 1.0f, 1.0f), Vector2(0.0f, 1.0f))
                <| Vertex(Vector4( 1.0f,  1.0f, 1.0f, 1.0f), Vector2(1.0f, 1.0f))

            // Top quad
            quad 
                <| Vertex(Vector4(-1.0f, 1.0f,  1.0f, 1.0f), Vector2(0.0f, 1.0f))
                <| Vertex(Vector4( 1.0f, 1.0f,  1.0f, 1.0f), Vector2(1.0f, 1.0f))
                <| Vertex(Vector4(-1.0f, 1.0f, -1.0f, 1.0f), Vector2(0.0f, 0.0f))
                <| Vertex(Vector4( 1.0f, 1.0f, -1.0f, 1.0f), Vector2(1.0f, 0.0f))

            // Bottom quad
            quad 
                <| Vertex(Vector4(-1.0f, -1.0f,  1.0f, 1.0f), Vector2(0.0f, 1.0f))
                <| Vertex(Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Vector2(0.0f, 0.0f))
                <| Vertex(Vector4( 1.0f, -1.0f,  1.0f, 1.0f), Vector2(1.0f, 1.0f))
                <| Vertex(Vector4( 1.0f, -1.0f, -1.0f, 1.0f), Vector2(1.0f, 0.0f))
        |] |> Array.concat

    let vertexSource = """
        #version 330

        uniform mat4 projection;
        
        layout (location = 0) in vec4 position;
        layout (location = 1) in vec2 uv;

        out vec2 fragmentUV;

        void main(void)
        {
            gl_Position = projection * position;
	        fragmentUV = uv;
        }
        """

    let fragmentSource = """
        #version 330
        
        in vec2 fragmentUV;

        uniform sampler2D textureSampler;

        out vec4 outputColor;

        void main(void)
        {
	        outputColor = texture(textureSampler, fragmentUV);
        }
        """

    let createShader typ source = 
        let shader = GL.CreateShader(typ)
        GL.ShaderSource(shader, source)
        GL.CompileShader(shader)
        let status = GL.GetShader(shader, ShaderParameter.CompileStatus)
        if status <> 0 then
            shader
        else
            failwith (GL.GetShaderInfoLog(shader))
            
    let mutable vertexShader = 0
    let mutable fragmentShader = 0
    let mutable program = 0

    let mutable projectionLocation = 0
    
    let mutable verticesVbo = 0
    let mutable vao = 0

    let mutable rotation = 0.0f
    let mutable yTranslate = 0.0f
    let mutable yBounce = true

    let mutable texture = 0

    override this.OnLoad(e) =
        vertexShader <- createShader ShaderType.VertexShader vertexSource

        fragmentShader <- createShader ShaderType.FragmentShader fragmentSource

        program <-
            let program = GL.CreateProgram()
            GL.AttachShader(program, vertexShader)
            GL.AttachShader(program, fragmentShader)
            GL.LinkProgram(program)
            let status = GL.GetProgram(program, GetProgramParameterName.LinkStatus)
            if status <> 0 then
                program
            else
                failwith (GL.GetProgramInfoLog(program))        

        verticesVbo <-
            let buffer = GL.GenBuffer()
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer)
            buffer
    
        vao <-
            let array = GL.GenVertexArray()
            GL.BindVertexArray(array)
            array

        // Get the uniform locations        
        projectionLocation <- GL.GetUniformLocation(program, "projection")
 
        // Transfer the vertices from CPU to GPU.
        GL.BufferData(BufferTarget.ArrayBuffer, nativeint(points.Length * sizeof<Vertex>), points, BufferUsageHint.StaticDraw)
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0)

        // Read texture file
        use bitmap = new System.Drawing.Bitmap("texture.bmp")
        let texture_data = 
            Array.init (bitmap.Width * bitmap.Height) (fun i ->
                let x = i % bitmap.Width
                let y = i / bitmap.Height
                let pixel = bitmap.GetPixel(x, y)
                [|pixel.R; pixel.G; pixel.B;|]
                ) |> Array.concat

        // Create a new texture
        texture <- GL.GenTexture()
        GL.BindTexture(TextureTarget.Texture2D, texture)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, texture_data)
        // Set texture wrapping and mip level bounds
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D)
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge)
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge)
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.LinearMipmapLinear);

        // Use the program.
        GL.UseProgram(program)
        let vertexPosition = GL.GetAttribLocation(program, "position");
        let vertexUv = GL.GetAttribLocation(program, "uv");
    
        GL.BindBuffer(BufferTarget.ArrayBuffer, verticesVbo)

        GL.VertexAttribPointer(vertexPosition, 4, VertexAttribPointerType.Float, false, sizeof<Vertex>, 0)
        GL.EnableVertexAttribArray(vertexPosition)

        GL.VertexAttribPointer(vertexUv, 2, VertexAttribPointerType.Float, false, sizeof<Vertex>, sizeof<Vector4>)
        GL.EnableVertexAttribArray(vertexUv)

        // Enable face culling
        GL.Enable(EnableCap.CullFace)
        GL.CullFace(CullFaceMode.Back)
        GL.FrontFace(FrontFaceDirection.Ccw)

        GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f)
        
    override this.OnUnload(e) = 
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0)
        GL.DeleteBuffer(verticesVbo)
        GL.DeleteVertexArray(vao)
        
        GL.BindTexture(TextureTarget.Texture2D, 0)
        GL.DeleteTexture(texture)

        GL.UseProgram(0)
        GL.DeleteProgram(program)
        GL.DeleteShader(vertexShader)
        GL.DeleteShader(fragmentShader)
        base.OnUnload(e)

    override this.OnResize(e) =
        GL.Viewport(0, 0, this.Width, this.Height)
        base.OnResize(e)

    override this.OnRenderFrame(e) =
        GL.Clear(ClearBufferMask.ColorBufferBit)

        rotation <- if rotation >= 360.0f then rotation - 360.0f else rotation + (10.0f * float32(e.Time))
        yBounce <- if yBounce then yTranslate >= -4.0f else yTranslate >= 4.0f
        yTranslate <- yTranslate + (if yBounce then -1.0f else +1.0f) * float32(e.Time)

        // Set projection matrix
        let projectionMatrix = 
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotation)) *
            Matrix4.CreateTranslation(0.0f, yTranslate, -4.0f) *
            Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90.0f), float32(this.ClientSize.Width) / float32(this.ClientSize.Height), 0.1f, 10.f)

        GL.UniformMatrix4(projectionLocation, false, ref projectionMatrix)

        // Set texture
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, texture)
        let textureLocation = GL.GetUniformLocation(program, "textureSampler")
        GL.ProgramUniform1(program, textureLocation, 0)

        GL.DrawArrays(PrimitiveType.Triangles, 0, points.Length)

        this.Context.SwapBuffers()
        base.OnRenderFrame(e)

[<EntryPoint>]
let main argv = 
    use window = new Window(640, 480, Graphics.GraphicsMode.Default, "Example Window", GameWindowFlags.Default)
    window.Run()
    0
