using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UniPlate : MonoBehaviour {

    protected const float BLOCK_SIZE_XZ   = 5f;
    protected const float QBLOCK_SIZE_XZ  = 2.5f;
    protected const int MAX_PLATE_SIZE_X  = 210 / 5;//210mmがタミヤが販売している最大サイズです
    protected const int MAX_PLATE_SIZE_Z  = 210 / 5;//210mmがタミヤが販売している最大サイズです
    protected const int DIV_DRAW_SIZE_XZ  = 16; //Mesh頂点数の限界(65536個)を鑑みた分割サイズ(平均64頂点 per 1/4Parts 程度で想定)

    /// <summary>
    /// 1/4 サイズ(1/2x1/2)のパーツデータ
    /// </summary>
    /// <remarks>
    /// この単位を扱うデータの最小単位とします
    /// </remarks>
    protected class QuarterInfo_
    {
        /// <summary>
        /// ブロックの形状
        /// </summary>
        public enum BlockTypes{
            /// <summary>穴用ブロック</summary>
            Hole,
            /// <summary>マージン用ブロック</summary>
            Margin,
        };
        /// <summary>
        /// ブロックの向き
        /// </summary>
        public enum RotateDirs
        {
            DEG_0   = 0,
            DEG_90  = 1,
            DEG_180 = 2,
            DEG_270 = 3,
        };
        public BlockTypes BlockType;
        public RotateDirs RotateDir;
    }
    protected class PlateDrawObject
    {
        public PlateDrawObject()
        {
            gameobject = new GameObject();
            gameobject.AddComponent<MeshFilter>();
            gameobject.AddComponent<MeshRenderer>();
            gameobject.AddComponent<BoxCollider>();
            gameobject.name = "PlateDrawObject";
        }
        public GameObject gameobject;
        public Mesh       mesh;
    }
    
    /// <summary>
    /// カット場所表す情報
    /// </summary>
    protected class CuttingInfo_
    {
        /// <summary>
        /// カット操作
        /// </summary>
        public enum CutOperations
        {
            /// <summary>何もしない</summary>
            None,
            /// <summary>カットする</summary>
            Cut,
        }

        public CuttingInfo_()
        {
            gameobject = new GameObject();
            gameobject.AddComponent<MeshFilter>();
            gameobject.AddComponent<MeshRenderer>();
            gameobject.name = "CuttingInfo";
       }

        #region horiCutLines_, vertCutLines_
        /// <summary>横線の指定</summary>
        /// <remarks>
        /// 繋がりを＃記号のように見た時の横線のカット位置指定です([縦ブロック数-1,横ブロック数])
        /// </remarks>
        protected CutOperations[,] horiCutLines_;

        /// <summary>縦線の指定</summary>
        /// <remarks>
        /// 繋がりを＃記号のように見た時の縦線のカット位置指定です([横ブロック数-1,縦ブロック数])
        /// </remarks>
        protected CutOperations[,] vertCutLines_;
        #endregion

        public GameObject gameobject;
        public Mesh mesh;

        public void SetPlateSize(int sizeX, int sizeZ)
        {
            horiCutLines_ = new CutOperations[sizeZ * 2 - 1, sizeX * 2];
            vertCutLines_ = new CutOperations[sizeX * 2 - 1, sizeZ * 2];
        }

        public void SetHoriLine_CutOperation_FromPlatePos(Vector2 platePosXZ, CutOperations cutOp)
        {
            var localX = platePosXZ.x;
            var localZ = platePosXZ.y;

            var horiLineIdx = (int)Mathf.Floor((localZ) / QBLOCK_SIZE_XZ - 0.5f);

            int horiSegIdx = (int)(localX / QBLOCK_SIZE_XZ);
            if (horiLineIdx >= 0 && horiLineIdx < horiCutLines_.GetLength(0) &&
               horiSegIdx >= 0 && horiSegIdx < horiCutLines_.GetLength(1))
            {
                horiCutLines_[horiLineIdx, horiSegIdx] = cutOp;
            }
        }

        public void SetVertLine_CutOperation_FromPlatePos(Vector2 platePosXZ, CutOperations cutOp)
        {
            var localX = platePosXZ.x;
            var localZ = platePosXZ.y;

            var vertLineIdx = (int)Mathf.Floor((localX) / QBLOCK_SIZE_XZ - 0.5f);

            int vertSegIdx = (int)(localZ / QBLOCK_SIZE_XZ);
            if (vertLineIdx >= 0 && vertLineIdx < vertCutLines_.GetLength(0) &&
                vertSegIdx >= 0 && vertSegIdx < vertCutLines_.GetLength(1))
            {
                vertCutLines_[vertLineIdx, vertSegIdx] = cutOp;
            }
        }

        public void SetCutOperationFromPlatePos(Vector2 platePosXZ, CutOperations cutOp)
        {
            var localX = platePosXZ.x;
            var localZ = platePosXZ.y;
            Debug.Log(string.Format("{0},{1}", localX, localZ));

            var horiLineIdx = (int)Mathf.Floor((localZ) / QBLOCK_SIZE_XZ - 0.5f);
            var vertLineIdx = (int)Mathf.Floor((localX) / QBLOCK_SIZE_XZ - 0.5f);
            var horiLineClip = (localZ - ((horiLineIdx + 1) * QBLOCK_SIZE_XZ)) / QBLOCK_SIZE_XZ;
            var vertLineClip = (localX - ((vertLineIdx + 1) * QBLOCK_SIZE_XZ)) / QBLOCK_SIZE_XZ;

            Debug.Log(string.Format("{0}, {1}  clip {2}, {3}", horiLineIdx, vertLineIdx, horiLineClip, vertLineClip));

            if (Mathf.Abs(horiLineClip) < Mathf.Abs(vertLineClip))
            {
                int horiSegIdx = (int)(localX / QBLOCK_SIZE_XZ);
                if(horiLineIdx>= 0 && horiLineIdx < horiCutLines_.GetLength(0)&&
                   horiSegIdx >= 0 && horiSegIdx  < horiCutLines_.GetLength(1))
                {
                    horiCutLines_[horiLineIdx, horiSegIdx] = cutOp;
                }
            }
            else
            {
                int vertSegIdx = (int)(localZ / QBLOCK_SIZE_XZ);
                if (vertLineIdx >= 0 && vertLineIdx < vertCutLines_.GetLength(0) &&
                    vertSegIdx  >= 0 && vertSegIdx  < vertCutLines_.GetLength(1))
                {
                    vertCutLines_[vertLineIdx, vertSegIdx] = cutOp;
                }
            }        
        }

        #region ReBuildDrawObject
        class DrawInfo_{
            public bool    bIsVert;
            public Vector2 pos;
            public CutOperations cutOpe;
        }

        public void ReBuildDrawObject()
        {
            if(null==vertCutLines_||null==horiCutLines_)return;
            var drawList = new List<DrawInfo_>();
            {
                for (int vIdx = 0; vIdx < horiCutLines_.GetLength(0); vIdx++)
                {
                    for (int hLineIdx = 0; hLineIdx < horiCutLines_.GetLength(1); hLineIdx++)
                    {
                        switch (horiCutLines_[vIdx, hLineIdx])
                        {
                        case CutOperations.Cut:
                            drawList.Add(new DrawInfo_(){
                                bIsVert = false,
                                pos = new Vector2(hLineIdx * QBLOCK_SIZE_XZ, vIdx * QBLOCK_SIZE_XZ + QBLOCK_SIZE_XZ),
                                cutOpe = horiCutLines_[vIdx, hLineIdx]
                            });
                            break;
                        }
                    }
                }
                for (int hIdx = 0; hIdx < vertCutLines_.GetLength(0); hIdx++)
                {
                    for (int vLineIdx = 0; vLineIdx < vertCutLines_.GetLength(1); vLineIdx++)
                    {
                        switch (vertCutLines_[hIdx, vLineIdx])
                        {
                        case CutOperations.Cut:
                            drawList.Add(new DrawInfo_()
                            {
                                bIsVert = true,
                                pos = new Vector2(hIdx * QBLOCK_SIZE_XZ + QBLOCK_SIZE_XZ, vLineIdx * QBLOCK_SIZE_XZ),
                                cutOpe = vertCutLines_[hIdx, vLineIdx]
                            });
                            break;
                        }
                    }
                }
            }
            mesh = new Mesh();
            {
                const float MIN_Y = -0.1f;
                const float MAX_Y =  3.1f;
                var vertices  = new Vector3[4 * 2      *2 * drawList.Count];
                var triangles = new int    [2 * 3 * 2  *2 * drawList.Count];
                var uv = new Vector2[vertices.Length];
                int baseIdx = 0;
                foreach(var draw in drawList)
                {
                    float addX      = draw.bIsVert ? 0 : QBLOCK_SIZE_XZ;
                    float addZ      = draw.bIsVert ? QBLOCK_SIZE_XZ : 0;
                    float lineWX    = draw.bIsVert ? -0.2f : 0;
                    float lineWZ    = draw.bIsVert ? 0     : 0.2f;
                    int baseVtxIdx = baseIdx * 16;
                    vertices[baseVtxIdx + 0] = new Vector3(draw.pos.x - lineWX,        MIN_Y, draw.pos.y - lineWZ);
                    vertices[baseVtxIdx + 1] = new Vector3(draw.pos.x - lineWX + addX, MIN_Y, draw.pos.y - lineWZ + addZ);
                    vertices[baseVtxIdx + 2] = new Vector3(draw.pos.x + lineWX + addX, MIN_Y, draw.pos.y + lineWZ + addZ);
                    vertices[baseVtxIdx + 3] = new Vector3(draw.pos.x + lineWX,        MIN_Y, draw.pos.y + lineWZ);
                    
                    vertices[baseVtxIdx + 4] = new Vector3(draw.pos.x - lineWX,        MAX_Y, draw.pos.y - lineWZ);
                    vertices[baseVtxIdx + 5] = new Vector3(draw.pos.x - lineWX + addX, MAX_Y, draw.pos.y - lineWZ + addZ);
                    vertices[baseVtxIdx + 6] = new Vector3(draw.pos.x + lineWX + addX, MAX_Y, draw.pos.y + lineWZ + addZ);
                    vertices[baseVtxIdx + 7] = new Vector3(draw.pos.x + lineWX,        MAX_Y, draw.pos.y + lineWZ);
                    SetRectangleVtx_(triangles, baseIdx * 8 + 0, baseVtxIdx + 0, baseVtxIdx + 1, baseVtxIdx + 2, baseVtxIdx + 3);
                    SetRectangleVtx_(triangles, baseIdx * 8 + 2, baseVtxIdx + 7, baseVtxIdx + 6, baseVtxIdx + 5, baseVtxIdx + 4);

                    baseVtxIdx += 8;
                    vertices[baseVtxIdx + 0] = new Vector3(draw.pos.x, MIN_Y, draw.pos.y);
                    vertices[baseVtxIdx + 1] = new Vector3(draw.pos.x + addX, MIN_Y, draw.pos.y + addZ);
                    vertices[baseVtxIdx + 2] = new Vector3(draw.pos.x + addX, MAX_Y, draw.pos.y + addZ);
                    vertices[baseVtxIdx + 3] = new Vector3(draw.pos.x, MAX_Y, draw.pos.y);

                    vertices[baseVtxIdx + 4] = vertices[baseVtxIdx + 0];
                    vertices[baseVtxIdx + 5] = vertices[baseVtxIdx + 1];
                    vertices[baseVtxIdx + 6] = vertices[baseVtxIdx + 2];
                    vertices[baseVtxIdx + 7] = vertices[baseVtxIdx + 3];
                    SetRectangleVtx_(triangles, baseIdx * 8 + 4, baseVtxIdx + 0, baseVtxIdx + 1, baseVtxIdx + 2, baseVtxIdx + 3);
                    SetRectangleVtx_(triangles, baseIdx * 8 + 6, baseVtxIdx + 7, baseVtxIdx + 6, baseVtxIdx + 5, baseVtxIdx + 4);

                    baseIdx++;
                }
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.uv = uv;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
            }
            var material = new Material(Shader.Find("Transparent/Diffuse"));
            {
                material.color = new Color(1,0,0,0.5f);
            }
            gameobject.GetComponent<MeshFilter>().sharedMesh = mesh;
            gameobject.GetComponent<MeshFilter>().sharedMesh.name = string.Format("cuttingMesh");
            gameobject.GetComponent<MeshRenderer>().material = material;
        }
        #endregion

        /// <summary>
        /// 横線のカット情報を取得
        /// </summary>
        /// <param name="lineIndex">横線の位置</param>
        /// <param name="xBlockIndex">横の対象のブロック位置</param>
        /// <returns></returns>
        public CutOperations GetHoriLineCutOp(int lineIndex, int xBlockIndex)
        {
            return horiCutLines_[lineIndex, xBlockIndex];
        }
        public CutOperations GetVertLineCutOp(int lineIndex,int zBlockIndex)
        {
            return vertCutLines_[lineIndex, zBlockIndex];
        }
    }

    protected QuarterInfo_[,]    plateBuildMap_;
    protected PlateDrawObject[,] plateDrawObjects_;
    protected CuttingInfo_       cutting_;

    #region mesh サポート
    static void SetRectangleVtx_(int[] triangles,int startTriIdx, int v0, int v1, int v2, int v3)
    {
        triangles[startTriIdx*3+0] = v0;
        triangles[startTriIdx*3+1] = v1;
        triangles[startTriIdx*3+2] = v2;
        triangles[startTriIdx*3+3] = v2;
        triangles[startTriIdx*3+4] = v3;
        triangles[startTriIdx*3+5] = v0;
    }
    #endregion

    #region CreateQuarterBlock_Hole()
    Mesh CreateQuarterBlock_Hole()
    {
        const float SIZE_XZ = 2.5f;
        const float MARGIN_LEN = 1.0f;
        const float HOLE_R = 1.5f;
        const float SIZE_Y = 3;
        const int DIV_R = 4;

        var mesh = new Mesh();

        var vertices  = new Vector3[6*2 + 4*4 + (DIV_R+1)*4];
        var triangles = new int[(((DIV_R)*4)+8*2) * 3];

        //MEMO: UnityのMESHは頂点法線を別インデックスとしては持てないみたいなので頂点多めに…

        //底面
        vertices[0] = new Vector3(0,0,0);
        vertices[1] = new Vector3(SIZE_XZ, 0, 0);
        vertices[2] = new Vector3(SIZE_XZ, 0, MARGIN_LEN);
        vertices[3] = new Vector3(MARGIN_LEN, 0, MARGIN_LEN);
        vertices[4] = new Vector3(MARGIN_LEN, 0, SIZE_XZ);
        vertices[5] = new Vector3(0, 0, SIZE_XZ);

        //上面
        vertices[6 + 0] = new Vector3(0, SIZE_Y, 0);
        vertices[6 + 1] = new Vector3(SIZE_XZ, SIZE_Y, 0);
        vertices[6 + 2] = new Vector3(SIZE_XZ, SIZE_Y, MARGIN_LEN);
        vertices[6 + 3] = new Vector3(MARGIN_LEN, SIZE_Y, MARGIN_LEN);
        vertices[6 + 4] = new Vector3(MARGIN_LEN, SIZE_Y, SIZE_XZ);
        vertices[6 + 5] = new Vector3(0, SIZE_Y, SIZE_XZ);

        // 側面1
        vertices[12+4*0+0] = vertices[0]; 
        vertices[12+4*0+1] = vertices[6+0]; 
        vertices[12+4*0+2] = vertices[6+1]; 
        vertices[12+4*0+3] = vertices[1];

        // 側面2
        vertices[12+4*1+0] = vertices[1]; 
        vertices[12+4*1+1] = vertices[6+1]; 
        vertices[12+4*1+2] = vertices[6+2]; 
        vertices[12+4*1+3] = vertices[2];

        // 側面3
        vertices[12+4*2+0] = vertices[0]; 
        vertices[12+4*2+1] = vertices[5]; 
        vertices[12+4*2+2] = vertices[6+5]; 
        vertices[12+4*2+3] = vertices[6+0];

        // 側面4
        vertices[12+4*3+0] = vertices[5]; 
        vertices[12+4*3+1] = vertices[4]; 
        vertices[12+4*3+2] = vertices[6+4]; 
        vertices[12+4*3+3] = vertices[6+5];

        // 穴の部分
        for (int ii = 0; ii < DIV_R+1; ii++)
        {
            var rC = Mathf.Cos((Mathf.PI / 2) * ((float)ii / DIV_R)) * HOLE_R;
            var rS = Mathf.Sin((Mathf.PI / 2) * ((float)ii / DIV_R)) * HOLE_R;

            vertices[12+16 + ii            ] = new Vector3(SIZE_XZ-rC, 0,      SIZE_XZ-rS);
            vertices[12+16 + ii + (DIV_R+1)] = new Vector3(SIZE_XZ-rC, SIZE_Y, SIZE_XZ-rS);
            vertices[12+16+(DIV_R+1)*2 + ii            ] = vertices[12+16 + ii            ];
            vertices[12+16+(DIV_R+1)*2 + ii + (DIV_R+1)] = vertices[12+16 + ii + (DIV_R+1)];
        }
        // 面
        SetRectangleVtx_(triangles, 0,  0, 1, 2, 3);
        SetRectangleVtx_(triangles, 2,  3, 4, 5, 0);
        SetRectangleVtx_(triangles, 4,  6 + 0,  6 + 3,  6 + 2,  6 + 1);
        SetRectangleVtx_(triangles, 6,  6 + 3,  6 + 0,  6 + 5,  6 + 4);

        int baseIdx=12;
        SetRectangleVtx_(triangles, 8, baseIdx + 0, baseIdx + 1, baseIdx + 2, baseIdx + 3);
        baseIdx += 4;
        SetRectangleVtx_(triangles,10, baseIdx + 0, baseIdx + 1, baseIdx + 2, baseIdx + 3);
        baseIdx += 4;
        SetRectangleVtx_(triangles,12, baseIdx + 0, baseIdx + 1, baseIdx + 2, baseIdx + 3);
        baseIdx += 4;
        SetRectangleVtx_(triangles,14, baseIdx + 0, baseIdx + 1, baseIdx + 2, baseIdx + 3);
        baseIdx += 4;

        for (int ii = 0; ii < DIV_R; ii++)
        {
            SetRectangleVtx_(triangles, 16+ii*4,
                baseIdx + ii,
                baseIdx + ii + 1,
                baseIdx + ii + (DIV_R + 1) + 1,
                baseIdx + ii + (DIV_R + 1));
            triangles[(16+ii*4+2) * 3 + 0] = 3;
            triangles[(16+ii*4+2) * 3 + 1] = baseIdx+(DIV_R + 1)*2 +ii+1;
            triangles[(16+ii*4+2) * 3 + 2] = baseIdx+(DIV_R + 1)*2 +ii;

            triangles[(16+ii*4+3) * 3 + 0] = 6 + 3;
            triangles[(16+ii*4+3) * 3 + 1] = baseIdx+(DIV_R + 1)*2 + ii + (DIV_R + 1);
            triangles[(16+ii*4+3) * 3 + 2] = baseIdx+(DIV_R + 1)*2 + ii + (DIV_R + 1)+1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
       
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;

    }
    #endregion

    #region CreateQuarterBlock_Margin()
    Mesh CreateQuarterBlock_Margin()
    {
        const float SIZE_XZ = 2.5f;
        const float SIZE_Y  = 3;

        var mesh = new Mesh();

        var vertices = new Vector3[4*6];
        var triangles = new int[6*3*2];

        //MEMO: UnityのMESHは頂点法線を別インデックスとしては持てないみたいなので頂点多めに…

        //底面
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(SIZE_XZ, 0, 0);
        vertices[2] = new Vector3(SIZE_XZ, 0, SIZE_XZ);
        vertices[3] = new Vector3(0, 0, SIZE_XZ);

        //上面
        vertices[4 + 0] = new Vector3(0, SIZE_Y, 0);
        vertices[4 + 1] = new Vector3(SIZE_XZ, SIZE_Y, 0);
        vertices[4 + 2] = new Vector3(SIZE_XZ, SIZE_Y, SIZE_XZ);
        vertices[4 + 3] = new Vector3(0, SIZE_Y, SIZE_XZ);

        // 側面1
        vertices[8 + 0] = vertices[0];
        vertices[8 + 1] = vertices[1];
        vertices[8 + 2] = vertices[4 + 1];
        vertices[8 + 3] = vertices[4 + 0];

        // 側面2
        vertices[12 + 0] = vertices[1];
        vertices[12 + 1] = vertices[2];
        vertices[12 + 2] = vertices[4 + 2];
        vertices[12 + 3] = vertices[4 + 1];

        // 側面3
        vertices[16 + 0] = vertices[2];
        vertices[16 + 1] = vertices[3];
        vertices[16 + 2] = vertices[4 + 3];
        vertices[16 + 3] = vertices[4 + 2];

        // 側面4
        vertices[20 + 0] = vertices[3];
        vertices[20 + 1] = vertices[0];
        vertices[20 + 2] = vertices[4 + 0];
        vertices[20 + 3] = vertices[4 + 3];

        // 面
        SetRectangleVtx_(triangles, 0,  0,  1,  2,  3);
        SetRectangleVtx_(triangles, 2,  7,  6,  5,  4);
        SetRectangleVtx_(triangles, 4, 11, 10,  9,  8);
        SetRectangleVtx_(triangles, 6, 15, 14, 13, 12 );
        SetRectangleVtx_(triangles, 8, 19, 18, 17, 16);
        SetRectangleVtx_(triangles,10, 23, 22, 21, 20);
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;

    }
    #endregion

    #region BuildMap_BasicPlate_ 形状を構築します
    /// <summary>
    /// 基本プレートを構築します
    /// </summary>
    /// <param name="plateSizeX">5mm(穴１個相当)を１単位としたXサイズを指定します</param>
    /// <param name="plateSizeY">5mm(穴１個相当)を１単位としたYサイズを指定します</param>
    /// <remarks>販売されている2.5mmの余白が付いたプレートを生成します</remarks>
    protected void BuildMap_BasicPlate_(int plateSizeX, int plateSizeZ)
    {
        int plateQSizeX = Mathf.Min(plateSizeX, MAX_PLATE_SIZE_X) * 2;
        int plateQSizeZ = Mathf.Min(plateSizeZ, MAX_PLATE_SIZE_Z) * 2;

        var hole4x4DirMap = new QuarterInfo_.RotateDirs[2, 2]{
            {QuarterInfo_.RotateDirs.DEG_0,  QuarterInfo_.RotateDirs.DEG_270,},
            {QuarterInfo_.RotateDirs.DEG_90, QuarterInfo_.RotateDirs.DEG_180,},
        };

        plateBuildMap_ = new QuarterInfo_[plateQSizeZ, plateQSizeX];
        for (int zz = 0; zz < plateBuildMap_.GetLength(0); zz++)
        {
            for (int xx = 0; xx < plateBuildMap_.GetLength(1); xx++)
            {
                if (xx == 0 || zz == 0 ||
                    (xx == plateBuildMap_.GetLength(1) - 1) ||
                    (zz == plateBuildMap_.GetLength(0) - 1))
                {
                    // 余白
                    plateBuildMap_[zz, xx] = new QuarterInfo_()
                    {
                        BlockType = QuarterInfo_.BlockTypes.Margin,
                    };
                }
                else
                {
                    // 穴
                    plateBuildMap_[zz, xx] = new QuarterInfo_()
                    {
                        BlockType = QuarterInfo_.BlockTypes.Hole,
                        RotateDir = hole4x4DirMap[(zz + 1) % 2, (xx + 1) % 2],
                    };
                }
            }
        }
        var boxcoll = GetComponent<BoxCollider>();
        boxcoll.size = new Vector3(plateQSizeX * QBLOCK_SIZE_XZ + 0.1f, 3 + +0.1f, plateQSizeZ * QBLOCK_SIZE_XZ + 0.1f);
        boxcoll.center = new Vector3(boxcoll.size.x / 2, boxcoll.size.y / 2, boxcoll.size.z / 2);

        ResizeCuttingInfoSize_();
    }
    #endregion

    #region RebuildDrawPlateObject_()
    /// <summary>
    /// プレートの表示物の再構築を行います。
    /// </summary>
    protected void RebuildDrawPlateObject_()
    {
        // 1/4パーツ合成用の格納場所を準備します
        var combineInsLsts = new List<CombineInstance>[plateDrawObjects_.GetLength(0),plateDrawObjects_.GetLength(1)];
        {
            for (int zz = 0; zz < plateDrawObjects_.GetLength(0); zz++)
            {
                for (int xx = 0; xx < plateDrawObjects_.GetLength(1); xx++)
                {
                    combineInsLsts[zz, xx] = new List<CombineInstance>();
                }
            }
        }
        // 1/4パーツのメッシュの生成
        for (int zzQ = 0; zzQ < plateBuildMap_.GetLength(0); zzQ++)
        {
            for (int xxQ = 0; xxQ < plateBuildMap_.GetLength(1); xxQ++)
            {
                var combineInsLst = combineInsLsts[zzQ / (DIV_DRAW_SIZE_XZ * 2), xxQ / (DIV_DRAW_SIZE_XZ * 2)];

                var qinfo = plateBuildMap_[zzQ, xxQ];
                if (qinfo == null)
                {
                    continue;
                }

                Matrix4x4 mtx;
                {
                    float roffsXZ = 2.5f / 2;
                    mtx = Matrix4x4.TRS(new Vector3(-roffsXZ, 0, -roffsXZ), Quaternion.identity, Vector3.one);
                    mtx = Matrix4x4.TRS(new Vector3(xxQ * 2.5f + roffsXZ, 0, zzQ * 2.5f + roffsXZ), Quaternion.Euler(0, (int)qinfo.RotateDir * 90, 0), Vector3.one) * mtx;
                }
                Mesh mesh = null;
                {
                    switch (qinfo.BlockType)
                    {
                        case QuarterInfo_.BlockTypes.Margin: mesh = CreateQuarterBlock_Margin(); break;
                        case QuarterInfo_.BlockTypes.Hole: mesh = CreateQuarterBlock_Hole(); break;
                    }
                }
                
                combineInsLst.Add(new CombineInstance()
                {
                    mesh = mesh,
                    transform = mtx,
                });
            }
        }
        // プレートのメッシュの構築
        for (int zz = 0; zz < plateDrawObjects_.GetLength(0); zz++)
        {
            for (int xx = 0; xx < plateDrawObjects_.GetLength(1); xx++)
            {
                var combineInsLst   = combineInsLsts[zz, xx];
                var plateDrawObject = plateDrawObjects_[zz, xx];

                plateDrawObject.mesh = new Mesh();
                plateDrawObject.mesh.CombineMeshes(combineInsLst.ToArray());
                plateDrawObject.gameobject.GetComponent<MeshFilter>().sharedMesh      = plateDrawObject.mesh;
                plateDrawObject.gameobject.GetComponent<MeshFilter>().sharedMesh.name = string.Format("plateMesh_{0}_{0}", zz, xx);
                plateDrawObject.gameobject.GetComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;
                plateDrawObject.gameobject.GetComponent<BoxCollider>().size = plateDrawObject.mesh.bounds.size;
                plateDrawObject.gameobject.GetComponent<BoxCollider>().center = plateDrawObject.mesh.bounds.center;
            }
        }
    }
    #endregion

    #region ResizeCuttingInfoSize_
    void ResizeCuttingInfoSize_()
    {
        // 
        cutting_ = new CuttingInfo_();
        cutting_.SetPlateSize(plateBuildMap_.GetLength(1) / 2, plateBuildMap_.GetLength(0) / 2);
        cutting_.gameobject.transform.parent = this.gameObject.transform;
        cutting_.ReBuildDrawObject();
    }
    #endregion

    
    #region 公開プロパティとメソッド

    public int PlateSizeBlockX = 32;
    public int PlateSizeBlockZ = 12;
    
    /// <summary>
    /// メインカメラの前に移動します
    /// </summary>
    public void MoveToMainCamForward(float forwardLen)
    {
        var u = Camera.main.transform.up;
        var r = Camera.main.transform.right;
        var f = Camera.main.transform.forward;
        var p = Camera.main.transform.position;
        
        var centerP = p +f * forwardLen;
        var sizeX   = PlateSizeBlockX * BLOCK_SIZE_XZ;
        var sizeZ   = PlateSizeBlockZ * BLOCK_SIZE_XZ;

        
        //ローカル座標系で中心に持ってくるための変換
        var offsRQ         = Quaternion.AngleAxis(90, Vector3.right);
        var offsMoveCenter = -Vector3.right * sizeX / 2.0f + -Vector3.forward * sizeZ / 2.0f;

        // カメラのむきに合わせた変換と合成してセットします
        this.transform.rotation = Camera.main.transform.rotation * offsRQ;
        this.transform.position = centerP + this.transform.rotation * offsMoveCenter;
    }

    #region カットモード BeginDrawPlateCuttingLine, ...
    class DrawCutLineWork_
    {
        public bool    bNowDrawLine_ = false;
        public Vector2 lastOnPlatePos_;
        public Vector2 lastDrawPlatePos_;
        public Vector2 lastDrawPlateDiffV_;
    }
    DrawCutLineWork_ drawCutLineWork_ = new DrawCutLineWork_();

    /// <summary>
    /// カットライン描画開始
    /// </summary>
    public bool BeginDrawPlateCuttingLine(Ray ray)
    {
        if (drawCutLineWork_.bNowDrawLine_) return false;

        if (RaycastPlateXZ_(ray, out drawCutLineWork_.lastOnPlatePos_))
        {
            drawCutLineWork_.bNowDrawLine_ = true;
            drawCutLineWork_.lastDrawPlateDiffV_ = new Vector2(0, 0);
            drawCutLineWork_.lastDrawPlatePos_ = drawCutLineWork_.lastOnPlatePos_;

            return true;
        }
        return false;
    }
    public void AddDrawPlateCuttingLine(Ray ray)
    {
        if (!drawCutLineWork_.bNowDrawLine_) return;

        var nowOnPlatePosXZ = new Vector2();
        if (false==RaycastPlateXZ_(ray, out nowOnPlatePosXZ))
        {
            return;
        }
        var diffV = nowOnPlatePosXZ - drawCutLineWork_.lastOnPlatePos_;
        if (diffV.magnitude > QBLOCK_SIZE_XZ / 2)
        {
            bool bDraw   = false;
            bool bIsVert = false;

            diffV.Normalize();
            //ある程度、縦横に方向がついていたら第一条件クリアです
            if (Mathf.Abs(diffV.x) > 0.8f)
            {
                bIsVert = false;
                if (Mathf.Abs(drawCutLineWork_.lastDrawPlateDiffV_.x) > 0.8f)
                {
                    // 前回と同じ方向ならば使用するラインをスナップします(違う方向を通さず直線を引く事は大抵意図と違うという想定）
                    drawCutLineWork_.lastOnPlatePos_.y = drawCutLineWork_.lastDrawPlatePos_.y;                    
                }
                else
                {
                    if (drawCutLineWork_.lastDrawPlateDiffV_.sqrMagnitude != 0)
                    {
                        // 前回と違う方向ならば前回の描画位置を起点と考えてはみ出る分を無視します
                        var dir = nowOnPlatePosXZ.x - drawCutLineWork_.lastDrawPlatePos_.x;
                        if (diffV.y > 0 != dir > 0)
                        {
                            bDraw = false;
                        }
                        else
                        {
                            //次の作業TODO; 描画位置の先端から繋がるようなラインに補正をする
                            drawCutLineWork_.lastOnPlatePos_ = drawCutLineWork_.lastDrawPlatePos_;
                        }
                    }
                }
                bDraw = true;
            }
            else if (Mathf.Abs(diffV.y) > 0.8f)
            {
                bIsVert = true;
                bDraw   = true;
                if (Mathf.Abs(drawCutLineWork_.lastDrawPlateDiffV_.y) > 0.8f)
                {
                    // 前回と同じ方向ならば使用するラインをスナップします(違う方向を通さず直線を引く事は大抵意図と違うという想定）
                    drawCutLineWork_.lastOnPlatePos_.x = drawCutLineWork_.lastDrawPlatePos_.x;
                    bDraw = true;
                }
                else
                {
                    if(drawCutLineWork_.lastDrawPlateDiffV_.sqrMagnitude!=0)
                    {
                        // 前回と違う方向ならば前回の描画位置を起点と考えてはみ出る分を無視します
                        var dir = nowOnPlatePosXZ.y - drawCutLineWork_.lastDrawPlatePos_.y;
                        if (diffV.y > 0 != dir > 0)
                        {
                            bDraw = false;
                        }
                        else
                        {
                            //次の作業TODO; 描画位置の先端から繋がるようなラインに補正をする
                            drawCutLineWork_.lastOnPlatePos_ = drawCutLineWork_.lastDrawPlatePos_;
                        }
                    }
                }
            }
            if (bDraw)
            {
                var drawNV = (drawCutLineWork_.lastOnPlatePos_ - drawCutLineWork_.lastDrawPlatePos_);
                var drawLen = drawNV.magnitude;
                drawNV.Normalize();
                for (float len = 0; len < drawLen; len += QBLOCK_SIZE_XZ / 2)
                {
                    if (bIsVert){
                        cutting_.SetVertLine_CutOperation_FromPlatePos(drawNV * len + drawCutLineWork_.lastDrawPlatePos_, CuttingInfo_.CutOperations.Cut);
                    }
                    else{
                        cutting_.SetHoriLine_CutOperation_FromPlatePos(drawNV * len + drawCutLineWork_.lastDrawPlatePos_, CuttingInfo_.CutOperations.Cut);
                    }
                }
                if (bIsVert){
                    cutting_.SetVertLine_CutOperation_FromPlatePos(drawCutLineWork_.lastOnPlatePos_, CuttingInfo_.CutOperations.Cut);
                }
                else{
                    cutting_.SetHoriLine_CutOperation_FromPlatePos(drawCutLineWork_.lastOnPlatePos_, CuttingInfo_.CutOperations.Cut);
                }
                cutting_.ReBuildDrawObject();
                drawCutLineWork_.lastDrawPlatePos_ = drawCutLineWork_.lastOnPlatePos_;
                drawCutLineWork_.lastOnPlatePos_ = nowOnPlatePosXZ;
                drawCutLineWork_.lastDrawPlateDiffV_ = diffV;
            }
        }
    }
    public void FinishDrawPlateCuttingLine()
    {
        drawCutLineWork_.bNowDrawLine_ = false;
    }
    #endregion

    /// <summary>
    /// カットを実行
    /// </summary>
    public void ExecuteCut()
    {
        int h = plateBuildMap_.GetLength(0);
        int w = plateBuildMap_.GetLength(1);
        
        var labelMap = new int[h,w];
        var labelLinkTbl = new Dictionary<int,int>();
        int labelSeed = 0;

        // ラベリングしていきます
        for (int yy = 0; yy < h; yy++)
        {
            if (yy == 0)
            {
                int nowLabel = labelSeed++;
                for (int xx = 0; xx < w; xx++)
                {
                    if (xx > 0)
                    {
                        if(cutting_.GetVertLineCutOp(xx-1, yy) == CuttingInfo_.CutOperations.Cut)
                        {
                            nowLabel = labelSeed++;
                        }
                    }
                    labelMap[yy, xx] = nowLabel;
                }
            }
            else
            {
                // 一つ上のブロックを起点にします
                int nowLabel = labelMap[yy-1, 0];
                for (int xx = 0; xx < w; xx++)
                {
                    if (cutting_.GetHoriLineCutOp(yy - 1, xx) == CuttingInfo_.CutOperations.None)
                    {
                        if (xx==0 || cutting_.GetVertLineCutOp(xx - 1, yy) == CuttingInfo_.CutOperations.None)
                        {
                            //上とつながる、左とつながる場合
                            if (labelMap[yy - 1, xx] != nowLabel)
                            {
                                // 上と違うラベルの場合、２つの領域が連結されます
                                labelLinkTbl[nowLabel] = labelMap[yy - 1, xx];
                            }
                        }
                        else
                        {
                            //上とつながる、左とつながら無い場合
                            nowLabel = labelMap[yy - 1, xx];
                        }
                    }
                    else
                    {
                        if (xx==0 || cutting_.GetVertLineCutOp(xx - 1, yy) == CuttingInfo_.CutOperations.None)
                        {
                            //上とつながら無い、左とつながる場合
                        }
                        else
                        {
                            //上とつながら無い、左とつながら無い場合
                            nowLabel = labelSeed++;
                        }
                    }
                    labelMap[yy, xx] = nowLabel;
                }    
            }
        }
        // ラベルを振りなおします
        var labelSet = new HashSet<int>();
        for (int yy = 0; yy < h; yy++)
        {
            for (int xx = 0; xx < w; xx++)
            {
                if (labelLinkTbl.ContainsKey(labelMap[yy, xx]))
                {
                    labelMap[yy, xx] = labelLinkTbl[labelMap[yy, xx]];
                }
                labelSet.Add(labelMap[yy, xx]);
            }
        }
        // 領域が２つ以上であれば分割します
        if(labelSet.Count>1)
        {
            Debug.Log(string.Format("labelSet.Count = {0}", labelSet.Count));

            for (int zz = 0; zz < h; zz++)
            {
                for (int xx = 0; xx < w; xx++)
                {
                    if(labelMap[zz, xx] != labelSet.GetEnumerator().Current)
                    {
                        plateBuildMap_[zz, xx] = null;
                    }
                }
            }        
        }


        RebuildDrawPlateObject_();
    }

    #endregion


    // Use this for initialization
    void Start()
    {
        // 表示物を入れるゲームオブジェクトなどを生成します
        plateDrawObjects_= new PlateDrawObject[(MAX_PLATE_SIZE_Z + DIV_DRAW_SIZE_XZ-1) / DIV_DRAW_SIZE_XZ,
                                               (MAX_PLATE_SIZE_X + DIV_DRAW_SIZE_XZ-1) / DIV_DRAW_SIZE_XZ];
        for (int zz = 0; zz < plateDrawObjects_.GetLength(0); zz++)
        {
            for (int xx = 0; xx < plateDrawObjects_.GetLength(1); xx++)
            {
                var drawObj = new PlateDrawObject();
                {
                    drawObj.gameobject.transform.parent = this.gameObject.transform;
                }
                plateDrawObjects_[zz, xx] = drawObj;
            }
        }
        //
        this.gameObject.AddComponent<BoxCollider>();
        
        //
        BuildMap_BasicPlate_(PlateSizeBlockX, PlateSizeBlockZ);

        //
        RebuildDrawPlateObject_();

    }
	
	// Update is called once per frame
	void Update () {
	}

    bool RaycastPlateXZ_(Ray ray,out Vector2 platePosXZ)
    {
        var plane0 = new Plane(transform.up, transform.position);
        var plane1 = new Plane(transform.up, transform.position + transform.up * 3);
        float hitDist = -1;
        {
            float dist0;
            float dist1;
            if (plane0.Raycast(ray, out dist0))
            {
                hitDist = dist0;
            }
            if (plane1.Raycast(ray, out dist1))
            {
                hitDist = Mathf.Min(dist0, dist1);
            }
        }
        if (hitDist > 0)
        {
            var hitV = (ray.direction * hitDist + ray.origin) - transform.position;
            var localX = Vector3.Dot(transform.right, hitV);
            var localZ = Vector3.Dot(transform.forward, hitV);

            platePosXZ = new Vector2(localX, localZ);
            return true;
        }
        else
        {
            platePosXZ = new Vector2(0, 0);
            return false;
        }
    }

    void OnMouseDown()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (BeginDrawPlateCuttingLine(ray))
        {
        }
    }

    void OnMouseDrag()
    {
        if(drawCutLineWork_.bNowDrawLine_)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            AddDrawPlateCuttingLine(ray);
        }
    }
    void OnMouseUp()
    {
        if (drawCutLineWork_.bNowDrawLine_)
        {
            FinishDrawPlateCuttingLine();
        }
    }
}
