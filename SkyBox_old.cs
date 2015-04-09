using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace XNADemo
{
    public class SkyBox : GameComponent, IDrawable
    {
        const int NUM_BG = 4;

        Texture2D[][] texture;
        Effect effect;
        VertexBuffer vb;
        IndexBuffer ib;
        VertexDeclaration vd;
        int currentBg;
        bool isChangeKeyPressed;

        public ContentManager content { get; set; }
        public Camera camera { get; set; }

        public SkyBox(Game g) : base(g)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            texture = new Texture2D[NUM_BG][];

            //Load textures of the skybox
            for (int i = 0; i < NUM_BG; i++)
            {
                texture[i] = new Texture2D[6];
                texture[i][0] = content.Load<Texture2D>("Textures/skybox/back"+i);
                texture[i][1] = content.Load<Texture2D>("Textures/skybox/front"+i);
                texture[i][2] = content.Load<Texture2D>("Textures/skybox/bottom"+i);
                texture[i][3] = content.Load<Texture2D>("Textures/skybox/top"+i);
                texture[i][4] = content.Load<Texture2D>("Textures/skybox/right"+i);
                texture[i][5] = content.Load<Texture2D>("Textures/skybox/left"+i);
            }
            //Load the shader
            effect = content.Load<Effect>("Shaders/Skybox");

            //Get a pointer to the graphics device so we can create the vertex and index buffers
            IGraphicsDeviceService graphicsService = (IGraphicsDeviceService)Game.Services.GetService(typeof(IGraphicsDeviceService));

            //create a vertex declaration with position and texture coordinates
            vd = new VertexDeclaration(
                graphicsService.GraphicsDevice, 
                new VertexElement[] {
                    new VertexElement(0, 0, 
                        VertexElementFormat.Vector3, 
                        VertexElementMethod.Default, 
                        VertexElementUsage.Position, 
                        0),
                    new VertexElement(0, sizeof(float)*3,
                        VertexElementFormat.Vector2,
                        VertexElementMethod.Default,
                        VertexElementUsage.TextureCoordinate,
                        0)
                });

            //create and populate the vertex buffer
            vb = new VertexBuffer(graphicsService.GraphicsDevice, typeof(VertexPositionTexture), 4 * 6, BufferUsage.WriteOnly);
            
            VertexPositionTexture[] data = new VertexPositionTexture[4 * 6];
            Vector3 vExtents = new Vector3(1,1,1);

            //back
            data[0].Position = new Vector3(vExtents.X, -vExtents.Y, -vExtents.Z);
            data[0].TextureCoordinate.X = 1.0f; data[0].TextureCoordinate.Y = 1.0f;
            data[1].Position = new Vector3(vExtents.X, vExtents.Y, -vExtents.Z);
            data[1].TextureCoordinate.X = 1.0f; data[1].TextureCoordinate.Y = 0.0f;
            data[2].Position = new Vector3(-vExtents.X, vExtents.Y, -vExtents.Z);
            data[2].TextureCoordinate.X = 0.0f; data[2].TextureCoordinate.Y = 0.0f;
            data[3].Position = new Vector3(-vExtents.X, -vExtents.Y, -vExtents.Z);
            data[3].TextureCoordinate.X = 0.0f; data[3].TextureCoordinate.Y = 1.0f;

            //front
            data[4].Position = new Vector3(-vExtents.X, -vExtents.Y, vExtents.Z);
            data[4].TextureCoordinate.X = 1.0f; data[4].TextureCoordinate.Y = 1.0f;
            data[5].Position = new Vector3(-vExtents.X, vExtents.Y, vExtents.Z);
            data[5].TextureCoordinate.X = 1.0f; data[5].TextureCoordinate.Y = 0.0f;
            data[6].Position = new Vector3(vExtents.X, vExtents.Y, vExtents.Z);
            data[6].TextureCoordinate.X = 0.0f; data[6].TextureCoordinate.Y = 0.0f;
            data[7].Position = new Vector3(vExtents.X, -vExtents.Y, vExtents.Z);
            data[7].TextureCoordinate.X = 0.0f; data[7].TextureCoordinate.Y = 1.0f;

            //bottom
            data[8].Position = new Vector3(-vExtents.X, -vExtents.Y, -vExtents.Z);
            data[8].TextureCoordinate.X = 1.0f; data[8].TextureCoordinate.Y = 0.0f;
            data[9].Position = new Vector3(-vExtents.X, -vExtents.Y, vExtents.Z);
            data[9].TextureCoordinate.X = 1.0f; data[9].TextureCoordinate.Y = 1.0f;
            data[10].Position = new Vector3(vExtents.X, -vExtents.Y, vExtents.Z);
            data[10].TextureCoordinate.X = 0.0f; data[10].TextureCoordinate.Y = 1.0f;
            data[11].Position = new Vector3(vExtents.X, -vExtents.Y, -vExtents.Z);
            data[11].TextureCoordinate.X = 0.0f; data[11].TextureCoordinate.Y = 0.0f;

            //top
            data[12].Position = new Vector3(vExtents.X, vExtents.Y, -vExtents.Z);
            data[12].TextureCoordinate.X = 0.0f; data[12].TextureCoordinate.Y = 0.0f;
            data[13].Position = new Vector3(vExtents.X, vExtents.Y, vExtents.Z);
            data[13].TextureCoordinate.X = 0.0f; data[13].TextureCoordinate.Y = 1.0f;
            data[14].Position = new Vector3(-vExtents.X, vExtents.Y, vExtents.Z);
            data[14].TextureCoordinate.X = 1.0f; data[14].TextureCoordinate.Y = 1.0f;
            data[15].Position = new Vector3(-vExtents.X, vExtents.Y, -vExtents.Z);
            data[15].TextureCoordinate.X = 1.0f; data[15].TextureCoordinate.Y = 0.0f;

            //left
            data[16].Position = new Vector3(-vExtents.X, vExtents.Y, -vExtents.Z);
            data[16].TextureCoordinate.X = 1.0f; data[16].TextureCoordinate.Y = 0.0f;
            data[17].Position = new Vector3(-vExtents.X, vExtents.Y, vExtents.Z);
            data[17].TextureCoordinate.X = 0.0f; data[17].TextureCoordinate.Y = 0.0f;
            data[18].Position = new Vector3(-vExtents.X, -vExtents.Y, vExtents.Z);
            data[18].TextureCoordinate.X = 0.0f; data[18].TextureCoordinate.Y = 1.0f;
            data[19].Position = new Vector3(-vExtents.X, -vExtents.Y, -vExtents.Z);
            data[19].TextureCoordinate.X = 1.0f; data[19].TextureCoordinate.Y = 1.0f;

            //right
            data[20].Position = new Vector3(vExtents.X, -vExtents.Y, -vExtents.Z);
            data[20].TextureCoordinate.X = 0.0f; data[20].TextureCoordinate.Y = 1.0f;
            data[21].Position = new Vector3(vExtents.X, -vExtents.Y, vExtents.Z);
            data[21].TextureCoordinate.X = 1.0f; data[21].TextureCoordinate.Y = 1.0f;
            data[22].Position = new Vector3(vExtents.X, vExtents.Y, vExtents.Z);
            data[22].TextureCoordinate.X = 1.0f; data[22].TextureCoordinate.Y = 0.0f;
            data[23].Position = new Vector3(vExtents.X, vExtents.Y, -vExtents.Z);
            data[23].TextureCoordinate.X = 0.0f; data[23].TextureCoordinate.Y = 0.0f;

            vb.SetData<VertexPositionTexture>(data);

            //create and populate the index buffer
            ib = new IndexBuffer(graphicsService.GraphicsDevice, typeof(short), 6 * 6, BufferUsage.WriteOnly);
            short[] indexData = new short[6 * 6];
            for (int x = 0; x < 6; x++)
            {
                indexData[x * 6 + 0] = (short)(x * 4 + 0);
                indexData[x * 6 + 2] = (short)(x * 4 + 1);
                indexData[x * 6 + 1] = (short)(x * 4 + 2);

                indexData[x * 6 + 3] = (short)(x * 4 + 2);
                indexData[x * 6 + 5] = (short)(x * 4 + 3);
                indexData[x * 6 + 4] = (short)(x * 4 + 0);
            }

            ib.SetData<short>(indexData);

            isChangeKeyPressed = false;
            currentBg = 0;
        }

        public void Draw(GameTime gameTime)
        {
            if (vb == null)
                return;

            IGraphicsDeviceService graphicsService = (IGraphicsDeviceService)Game.Services.GetService(typeof(IGraphicsDeviceService));
            GraphicsDevice device = graphicsService.GraphicsDevice;
            device.RenderState.DepthBufferWriteEnable = false;

            effect.Begin();
            {
                //make the skybox to move along with the camera.
                Matrix world = camera.WorldMatrix * Matrix.CreateTranslation(camera.CameraPosition);
                Matrix view = camera.ViewMatrix;
                Matrix proj = camera.ProjectionMatrix;
                effect.Parameters["worldViewProjection"].SetValue(world * view * proj);

                for (int x = 0; x < 6; x++)
                {
                    device.VertexDeclaration = vd;
                    device.Vertices[0].SetSource(vb, 0, vd.GetVertexStrideSize(0));
                    device.Indices = ib;

                    effect.Parameters["baseTexture"].SetValue(texture[currentBg][x]);
                    effect.Techniques[0].Passes[0].Begin();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, x * 4, 4, x * 6, 2);
                    effect.Techniques[0].Passes[0].End();
                }
            }
            effect.End();

            device.RenderState.DepthBufferWriteEnable = true;
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState keyState = Keyboard.GetState();

            if (keyState.IsKeyDown(Keys.B))
            {
                ChangeBackground();
                //avoid changing model again while key is pressed
                isChangeKeyPressed = true;
            }
            if (keyState.IsKeyUp(Keys.B))
                isChangeKeyPressed = false;

            base.Update(gameTime);
        }

        void ChangeBackground()
        {
            if (isChangeKeyPressed)
                return;

            currentBg++;
            if (currentBg >= NUM_BG)
                currentBg = 0;
        }

        public int DrawOrder
        {
            get { return 0; }
        }

        public event System.EventHandler DrawOrderChanged;

        public bool Visible
        {
            get { return true; }
        }

        public event System.EventHandler VisibleChanged;
    }
}
