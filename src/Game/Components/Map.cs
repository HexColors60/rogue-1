﻿#region copyright
// Copyright (C) 2018 "Daniel Bramblett" <bram4@pdx.edu>, "Daniel Dupriest" <kououken@gmail.com>, "Brandon Goldbeck" <bpg@pdx.edu>
// This software is licensed under the MIT License. See LICENSE file for the full text.
#endregion

using System;
using System.Collections.Generic;

using Ecs;
using Game.DungeonMaker; 
using IO;

namespace Game.Components
{    


    public class Map : Component
    {
        /// <summary>
        /// An enumeration used by the Map class to mark grid spaces as walls or not.
        /// </summary>
        public enum CellState { Open, Blocked };

        public int width;
        public int height;
        private List<List<GameObject>> objects;
        public int startingX = 0;
        public int startingY = 0;
        //private static Map map = null;

        public Map(int width, int height, int level)
        {
            this.width = width;
            this.height = height;

            // Initialize object storage
            objects = new List<List<GameObject>>();
            for (int x = 0; x < width; ++x)
            {
                List<GameObject> row = new List<GameObject>();
                for (int y = 0; y < height; ++y)
                {
                    row.Add(null);
                }
                objects.Add(row);
            }
        }

        /*public static Map CacheInstance()
        {
            return map;
        }*/

        public override void Start()
        {
            /*if (map != null && map != this)
            {
                GameObject.Destroy(this.gameObject);
            }
            else
            {
                map = this;
            }
            CreateLevel(1);
            return;*/
            //CreateLevel(1);
        }

        public override void Update()
        {
            return;
        }

        public override void Render()
        {
            return;
        }

        public override void OnResize()
        {
            //transform.position.y = ConsoleUI.MaxHeight() - 1;
            transform.position = new Vec2i(transform.position.x, ConsoleUI.MaxHeight() - 1);
            return;
        }

        /// <summary>
        /// Create a new generated map and fill it with enemies and other good stuff.
        /// </summary>
        /// <param name="level">The difficulty level of the enemies created.</param>
        public void CreateLevel(int level)
        {
            Clear();
            BasicDungeon dm = new BasicDungeon(this.width, this.height, (int)DateTime.Now.Ticks);
            dm.Generate();


            List<List<String>> blueprint = dm.Package();
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    switch (blueprint[x][y])
                    {
                        case "d":
                            objects[x][y] = SpawnManager.CreateDoor(this.gameObject, x, y);
                            //objects[x][y].Transform.SetParent(this.gameObject.Transform);
                            break;
                        case "l":
                            objects[x][y] = SpawnManager.CreateLockedDoor(this.gameObject, x, y);
                            //objects[x][y].Transform.SetParent(this.gameObject.Transform);
                            break;
                        case "m":
                            objects[x][y] = SpawnManager.CreateEnemy(this.gameObject, x, y, level);
                            //objects[x][y].Transform.SetParent(this.gameObject.Transform);
                            break;
                        case "k":
                            objects[x][y] = SpawnManager.CreateEnemy(this.gameObject, x, y, level, true);
                            //objects[x][y].Transform.SetParent(this.gameObject.Transform);
                            break;
                        case "b":
                            objects[x][y] = SpawnManager.CreateBoss(this.gameObject, x, y, level);
                            //objects[x][y].Transform.SetParent(this.gameObject.Transform);
                            break;
                        case "w":
                            objects[x][y] = SpawnManager.CreateWall(this.gameObject, x, y,(x == 0) || (y == 0) 
                                || (x + 1  == width) || (y + 1 == height));
                            //objects[x][y].Transform.SetParent(this.gameObject.Transform);
                            break;
                        case "s":
                            this.startingX = x;
                            this.startingY = y;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Destroy the grid of game objects, so we can reload a new map later.
        /// </summary>
        public void Clear()
        {

        }

        public GameObject PeekObject(int x, int y)
        {
            //Debug.Log("PeekObject called with x = " + x + ", y = " + y + ".");
            if (x < 0 || x >= width || y < 0 || y >= height)
                return null;
            return objects[x][y];
        }

        public GameObject PeekObject(Vec2i location)
        {
            if (location == null)
                return null;
            return PeekObject(location.x, location.y);
        }

        public GameObject PopObject(int x, int y)
        {
            //Debug.Log("PopObject called with x = " + x + ", y = " + y + ".");
            if (x < 0 || x >= width || y < 0 || y >= height)
                return null;
            GameObject result = objects[x][y];
            objects[x][y] = null;
            return result;
        }

        public bool AddObject(int x, int y, GameObject go)
        {
            if (objects[x][y] != null)
            {
                Debug.LogError("Cant add game object" + go.Name + " to map, location is already occupied.");
                return false;
            }
            
            objects[x][y] = go;
            return true;
        }

        public override void OnDestroy()
        {
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    if(objects[x][y] != null)
                    {
                        //GameObject.Destroy(objects[x][y]);
                        objects[x][y] = null;
                    }
                }
            }
            return;
        }
    }
}