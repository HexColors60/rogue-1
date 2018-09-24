﻿#region copyright
// Copyright (C) 2018 "Daniel Bramblett" <bram4@pdx.edu>, "Daniel Dupriest" <kououken@gmail.com>, "Brandon Goldbeck" <bpg@pdx.edu>
// This software is licensed under the MIT License. See LICENSE file for the full text.
#endregion

using Ecs;
using IO;

namespace Game.Components
{
    class GameManager : Component
    {
        private const int hudWidth = 36;
        public int gameWidth;
        public int gameHeight;

        public Player player;
        public MapManager mapManager;
        public HUD hud;
        public Map currentMap;

        public GameManager(int width, int height)
        {
            this.gameWidth = width;
            this.gameHeight = height;

        }

        public override void Start()
        {
            player = (Player)GameObject.Instantiate(this.gameObject,"MainPlayer").AddComponent(new Player("Sneaky McDevious", "Thiefy rogue", 1, 10, 1, 2));
            player.Name = "MainPlayer";

            GameObject mapObject = GameObject.Instantiate("MapManager");
            mapObject.Name = "MapManager";

            mapManager = (MapManager)mapObject.AddComponent(new MapManager(80, 40, 1));

            mapObject.Transform.position = new Vec2i(mapObject.Transform.position.x, gameHeight - 1);

            Map map = MapManager.CurrentMap();
            currentMap = map;
           
            
            player.AddComponent(new PlayerController());
            player.AddComponent(new Model());
            player.AddComponent(new LightSource(10.0f));
            player.AddComponent(new Camera(gameWidth - hudWidth, gameHeight));
            player.AddComponent(new MapTile('$', new Color(255, 255, 255), true));
            player.AddComponent(new Inventory());
            player.AddComponent(new Sound());

            player.transform.position = new Vec2i(map.startingX, map.startingY);
            //player.transform.SetParent(this.transform);

            map.AddObject(map.startingX, map.startingY, player.gameObject);
  

            // Setup HUD for stats and info
            hud = (HUD)GameObject.Instantiate("HUD").AddComponent(new HUD(hudWidth, gameHeight));
            hud.Name = "HUD";

            hud.transform.SetParent(this.transform);

            Model hudModel = (Model)hud.AddComponent(new Model());
            hudModel.color.Set(180, 180, 180);
            hud.transform.position = new Vec2i(gameWidth - hudWidth, gameHeight - 1);

            Debug.Log("GameManager added all components on start.");
            return;
        }

        public override void OnResize()
        {
            gameWidth = ConsoleUI.MaxWidth();
            gameHeight = ConsoleUI.MaxHeight();

            //Camera camera = (Camera)GameObject.FindWithTag("Player").GetComponent(typeof(Camera));

            Camera camera = Camera.MainCamera();
            camera.Resize(gameWidth - hudWidth, gameHeight);

            GameObject hudObject = GameObject.FindWithTag("HUD");

            hudObject.Transform.position = new Vec2i(gameWidth - hudWidth, gameHeight - 1);
            

            HUD hud = (HUD)hudObject.GetComponent(typeof(HUD));
            hud.Resize(hudWidth, gameHeight);

            Debug.Log("Resized game to " + gameWidth + " x " + gameHeight + ".");
        }

        public override void Update()
        {
            return;
        }

        public override void LateUpdate()
        {
            return;
        }

        public override void OnDestroy()
        {
            //GameObject.Destroy(currentMap.gameObject);
            GameObject.Destroy(mapManager.gameObject);
            
            return;
        }

    }
}
