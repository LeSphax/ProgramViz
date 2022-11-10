using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;
using static UnityEditor.PlayerSettings;
using UnityEngine.UI;

public class Graph : MonoBehaviour
{
    public float idealDistance = 3f;
    public float speed = 3f;
    public GameObject node;
    List<Module> modules = new List<Module>();
    Dictionary<string, Module> existingModules = new Dictionary<string, Module>();

    Module addModule(ParsedModule parsedModule, ParsedModule[] parsedModules)
    {
        List<Module> dependencies = new List<Module>();
        Module newModule = new Module(parsedModule.source);
        modules.Add(newModule);
        if (existingModules.ContainsKey(newModule.source))
        {
            return null;
        }
        existingModules.Add(newModule.source, newModule);
        for (int i = 0; i < parsedModule.dependencies.Length; i++)
        {
            string name = parsedModule.dependencies[i].resolved;
            if (existingModules.ContainsKey(name))
            {
                dependencies.Add(existingModules[name]);
                existingModules[name].dependents.Add(newModule);
            }
            else
            {

                dynamic dependency = null;
                for (int j = 0; j < parsedModules.Length; j++)
                {
                    if (parsedModules[j].source == name)
                    {
                        dependency = parsedModules[j];
                        break;
                    }
                }
                if (dependency == null)
                {
                    Debug.Log("Dependency not found: " + name);
                    continue;
                }

                Module dependencyModule = addModule(dependency, parsedModules);
                if (dependencyModule != null)
                {
                    dependencies.Add(dependencyModule);
                    dependencyModule.dependents.Add(newModule);
                }
            }
        }

        newModule.dependencies = dependencies;
        return newModule;
    }

    Vector3 randomStartPos()
    {
        return new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, 0);
    }

    void Start()
    {
        //Parse the JSON file deps.json

        return;
        Timing.Start("Read JSON");
        string json = System.IO.File.ReadAllText("Assets/deps.json");
        Timing.Stop("Read JSON");
        Timing.Start("Parse JSON");
        Content d = JsonConvert.DeserializeObject<Content>(json);
        Timing.Stop("Parse JSON");
        Timing.Start("Modules");
        for (int i = 0; i < d.modules.Length; i++)
        {
            ParsedModule module = d.modules[i];
            if (module.source != null && !existingModules.ContainsKey(module.source))
            {
                //if (module.source.StartsWith("src/types")) continue;
                addModule(d.modules[i], d.modules);
            }
        }
        Debug.Log(d.modules.Length);
        Timing.Stop("Modules");

        Timing.Start("GameObjects");
        for (int i = 0; i < modules.Count; i++)
        {
            GameObject instance = Instantiate(node, randomStartPos(), Quaternion.identity);
            instance.name = modules[i].source;
            if (modules[i].dependents.Count == 0)
            {
                instance.GetComponent<Renderer>().material.color = Color.red;
            }
            else if (modules[i].dependencies.Count == 0)
            {
                instance.GetComponent<Renderer>().material.color = Color.green;
            }
            else
            {
                instance.GetComponent<Renderer>().material.color = Color.blue;
            }

            //instance.GetComponentInChildren<Canvas>().worldCamera = Camera.main;
            //var split = modules[i].source.Split('/');
            //instance.GetComponentInChildren<Text>().text = split[split.Length - 1];
            modules[i].node = instance;
        }
        Timing.Stop("GameObjects");
    }

    Vector3 attract(Vector3 currentPos, Vector3 targetPos, bool draw = false)
    {
        Timing.Start("Distance");
        float distance = Vector3.Distance(currentPos, targetPos);
        Timing.Batch("Distance");

        Timing.Start("Direction");
        Vector3 direction = (targetPos - currentPos).normalized;
        if (direction.magnitude < 0.5f)
        {
            direction = randomStartPos();
        }
        Timing.Batch("Direction");


        Timing.Start("Force");
        Vector3 force = Mathf.Min(distance * distance / idealDistance, distance) * direction;
        Timing.Batch("Force");
        //if (draw) DrawArrow.ForDebug(currentPos, targetPos);
        return force;
    }

    void Update()
    {
        //Timing.Start("Repulse");
        //for (int i = 0; i < modules.Count; i++)
        //{
        //    Module current = modules[i];
        //    Vector3 currentChange = Vector3.one * 0.00001f;
        //    Vector3 currentPos = current.node.transform.position;

        //    //for (int j = 0; j < modules.Count; j++)
        //    //{
        //    //    if (i == j) continue;

        //    //    //Vector3 targetPos = modules[j].node.transform.position;
        //    //    //Vector3 direction = (currentPos - targetPos).normalized;
        //    //    //if (direction.magnitude < 0.5f)
        //    //    //{
        //    //    //    direction = randomStartPos();
        //    //    //}
        //    //    //float distance = Vector3.Distance(currentPos, targetPos);
        //    //    //Vector3 force = (idealDistance * idealDistance / (distance * distance)) * direction;
        //    //    currentChange += Vector3.one * 0.00001f;
        //    //}


        //    Timing.Start("Attract Loop");
        //    for (int j = 0; j < current.dependencies.Count; j++)
        //    {

        //        Vector3 targetPos = current.dependencies[j].node.transform.position;
        //        currentChange += attract(currentPos, targetPos, true);
        //    }
        //    Timing.Batch("Attract Loop");

        //    //for (int j = 0; j < current.dependents.Count; j++)
        //    //{
        //    //    Vector3 targetPos = current.dependents[j].node.transform.position;
        //    //    currentChange += attract(currentPos, targetPos);
        //    //}

        //    //if (current.dependents.Count == 0)
        //    //{
        //    //    currentChange += attract(currentPos, Vector3.zero, true);
        //    //}

        //    currentChange = currentChange.normalized * Mathf.Min(currentChange.magnitude, idealDistance * 3);
        //    current.node.transform.position += currentChange * Time.deltaTime * speed;

        //    current.previousAcc = currentChange;
        //}
        //Timing.StopBatch("Distance");
        //Timing.StopBatch("Force");
        //Timing.StopBatch("Direction");
        //Timing.StopBatch("Attract Loop");
        //Timing.Stop("Repulse");
    }
}

public class Module
{
    public string source;
    public List<Module> dependencies = new List<Module>();
    public List<Module> dependents = new List<Module>();
    public GameObject node;
    public Vector3 previousAcc;


    public Module(string source)
    {
        this.source = source;
    }
}


public class ParsedDependency
{
    public string resolved;

    public ParsedDependency(string resolved)
    {
        this.resolved = resolved;
    }
}

public class ParsedModule
{
    public string source;
    public ParsedDependency[] dependencies;

    public ParsedModule(string source, ParsedDependency[] dependencies)
    {
        this.source = source;
        this.dependencies = dependencies;
    }
}

public class Content
{
    public ParsedModule[] modules;

    public Content(ParsedModule[] modules)
    {
        this.modules = modules;
    }
}
