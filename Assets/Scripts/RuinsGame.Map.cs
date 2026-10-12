using System.Collections.Generic;
using Portfolio.Domain;
using UnityEngine;

public sealed partial class RuinsGame
{
    readonly List<SpriteRenderer> mapTiles=new List<SpriteRenderer>();
    readonly List<GameObject> mapDecor=new List<GameObject>();
    Material groundMaterial;
    Sprite boundaryCoping,boundaryWall;
    Vector2 cameraFocus=ArenaLayout.Entrance;
    string RegionName=>player.y<700?"北庭 · 传送祭坛":player.x<1050?"西侧 · 苔藓遗迹":player.x>2150?"东侧 · 破碎石庭":"中央 · 行军大道";
    void InitializeMap()
    {
        // Interior crops remove atlas gutters. Reuse sprites and tile renderers across rooms.
        foreach(var name in new[]{"floor_r1_c1","floor_r1_c2","floor_r1_c3","floor_r1_c4","floor_r1_c5","floor_r2_c2","floor_r4_c1","floor_r4_c3","floor_r4_c4","floor_r4_c5"})
        {
            var source=Art("floor/"+name);var r=source.rect;
            r=new Rect(r.x+r.width*.105f,r.y+r.height*.105f,r.width*.79f,r.height*.79f);
            floorSprites.Add(Sprite.Create(source.texture,r,new Vector2(.5f,.5f),1,0,SpriteMeshType.FullRect));
        }
        for(int y=0;y<28;y++)for(int x=0;x<40;x++)
            mapTiles.Add(Make("floor/floor_r1_c1",new Vector2(x*80+40,y*80+40),80,-100));
        var shader=Resources.Load<Shader>("Map/RuinsGround");
        if(shader==null || !shader.isSupported){Debug.LogError("Ruins ground shader unavailable");enabled=false;return;}
        groundMaterial=new Material(shader);
        groundMaterial.SetTexture("_BaseTex",floorSprites[0].texture);
        groundMaterial.SetTexture("_MossTex",floorSprites[4].texture);
        groundMaterial.SetTexture("_CrackTex",floorSprites[8].texture);
        var bounds=ArenaLayout.Bounds;
        groundMaterial.SetVector("_Playable",new Vector4(bounds.xMin,bounds.yMin,bounds.xMax,bounds.yMax));
        // Reuse the opaque top of the existing wall as a low perimeter coping.
        var wall=Art("walls/wall_straight");var wr=wall.rect;
        // Crop inside opaque stone, not the transparent padding around the original sprite.
        boundaryCoping=Sprite.Create(wall.texture,new Rect(wr.x+32,wr.y+142,wr.width-64,40),new Vector2(.5f,.5f),1,0,SpriteMeshType.FullRect);
        boundaryWall=Sprite.Create(wall.texture,new Rect(wr.x+30,wr.y+16,wr.width-60,wr.height-36),new Vector2(.5f,0),1,0,SpriteMeshType.FullRect);
        RebuildMap();
    }
    void MapProp(string key,Vector2 p,float width)
    {
        mapDecor.Add(Make(key,p,width,(int)p.y).gameObject);spawnExclusions.Add(p);
    }
    void RebuildMap()
    {
        foreach(var go in mapDecor){go.SetActive(false);Destroy(go);}mapDecor.Clear();spawnExclusions.Clear();
        int theme=Mathf.Max(1,room);groundMaterial.SetFloat("_Theme",theme);
        for(int y=0;y<28;y++)for(int x=0;x<40;x++)
        {
            int hash=(x*73+y*131+theme*31)%100;
            // Small stone variation underneath continuous world-space biome masks.
            int tile=hash<75?0:hash<85?1:hash<93?5:6;
            var view=mapTiles[y*40+x];view.sprite=floorSprites[tile];
            view.transform.localScale=new Vector3(80.5f/view.sprite.rect.width,80.5f/view.sprite.rect.height,1);
            view.color=Color.white;view.sharedMaterial=groundMaterial;
        }
        var bounds=ArenaLayout.Bounds;
        float wallLength=bounds.width+48;int wallCount=Mathf.CeilToInt(wallLength/260);float wallStep=wallLength/wallCount;
        for(int i=0;i<wallCount;i++)
        {
            var view=Make("walls/wall_straight",new Vector2(bounds.xMin-24+(i+.5f)*wallStep,bounds.yMin),285,(int)bounds.yMin);
            view.name="Boundary wall";view.sprite=boundaryWall;view.transform.localScale=Vector3.one*((wallStep+3)/boundaryWall.rect.width);mapDecor.Add(view.gameObject);
        }
        AddCoping(new Vector2(bounds.xMin-12,bounds.yMin),new Vector2(bounds.xMin-12,bounds.yMax+24));
        AddCoping(new Vector2(bounds.xMax+12,bounds.yMin),new Vector2(bounds.xMax+12,bounds.yMax+24));
        AddCoping(new Vector2(bounds.xMin-24,bounds.yMax+12),new Vector2(bounds.xMax+24,bounds.yMax+12));
        // Low stone borders meet the collision boundary; torches only provide landmarks.
        for(int y=480;y<2160;y+=400)
        {
            MapProp("props/torch",new Vector2(100,y),65);
            MapProp("props/torch",new Vector2(3100,y),65);
        }
        MapProp("props/banner",new Vector2(1410,320),70);
        MapProp("props/banner",new Vector2(1790,320),70);
        MapProp("props/torch",new Vector2(1430,1770),65);
        MapProp("props/torch",new Vector2(1770,1770),65);
        cameraFocus=player;
    }
    void AddCoping(Vector2 start,Vector2 end)
    {
        float length=Vector2.Distance(start,end);int count=Mathf.CeilToInt(length/180);
        for(int i=0;i<count;i++)
        {
            var go=new GameObject("Boundary coping");go.transform.SetParent(transform,false);
            var view=go.AddComponent<SpriteRenderer>();view.sprite=boundaryCoping;
            float segment=length/count;
            view.transform.localScale=new Vector3((segment+1)/boundaryCoping.rect.width,24/boundaryCoping.rect.height,1);
            view.transform.rotation=Quaternion.Euler(0,0,-Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg);
            Place(view,Vector2.Lerp(start,end,(i+.5f)/count),-40);mapDecor.Add(go);
        }
    }
    void DisposeMap()
    {
        if(groundMaterial!=null)Destroy(groundMaterial);
        if(boundaryCoping!=null)Destroy(boundaryCoping);
        if(boundaryWall!=null)Destroy(boundaryWall);
    }
    Vector3 FollowCameraPosition()
    {
        float halfY=gameCamera.orthographicSize;
        float halfX=halfY*gameCamera.aspect;
        float x=halfX>=ArenaLayout.Width*.5f?ArenaLayout.Width*.5f:Mathf.Clamp(cameraFocus.x,halfX,ArenaLayout.Width-halfX);
        float y=Mathf.Clamp(cameraFocus.y,halfY,ArenaLayout.Height-halfY);
        return new Vector3(x,720-y,-10);
    }
}
