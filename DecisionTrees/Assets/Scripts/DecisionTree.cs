using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class DecisionTree : MonoBehaviour {

    public TextAsset sourceText;
    public GameObject nodeToDraw;
    public GameObject linkToDraw;

    private string text;

    //private Dictionary<string, string[]> l_values;

    private List<string> labels;
    private List<List<string>> values;
    private List<List<string>> otherWayValues;

    //FINAL TREE
    private List<List<String[]>> decisionTree;

	// Use this for initialization
	void Start () {
        labels = new List<string>();
        values = new List<List<string>>();

        decisionTree = new List<List<string[]>>(); //value,link,parent,leaf


        text = sourceText.text;
        n_ProcessSourceText(text);
        //ProcessSourceText(text);

        List<string> n_labels = new List<string>();
        foreach(string actual in labels)
        {
            n_labels.Add(actual);
        }

        List<List<string>> n_values = new List<List<string>>();
        for(int i=0;i<values.Count;i++)
        {
            n_values.Add(new List<string>());
            foreach (string localString in values[i])
            {
                n_values[i].Add(localString);
            }
        }

        /*
        Hay que hacer que o bien se trabaje con valores y no con referencias
        O olvidarse de la recursividad de algún modo o yo que sé
        pero al eliminar de una rama no se puede eliminar de todo los labels porque se lia
        List<List<string>> n_values = DeepCopy(values);
        */

        //CreateTree(n_labels, n_values,0, decisionTree);
        n_CreateTree(n_labels, n_values, 0, decisionTree, "raiz", "raiz");
        DrawTree();
        int a = 2;
    }

    private void DrawTree()
    {
        GameObject canvas = GameObject.Find("Canvas");
        float width = canvas.GetComponent<RectTransform>().rect.width;
        float height = canvas.GetComponent<RectTransform>().rect.height;

        List<List<GameObject>> nodes = new List<List<GameObject>>();
        List<List<int>> links = new List<List<int>>();

        for (int i=0;i<decisionTree.Count;i++)
        {
            nodes.Add(new List<GameObject>());
            for(int j=0;j<decisionTree[i].Count;j++)
            {
                GameObject node = Instantiate(nodeToDraw, canvas.transform);
                node.transform.GetChild(0).GetComponent<Text>().text = decisionTree[i][j][0];
                node.transform.position = new Vector3( width/2 + j * 200, height - 75 - 75 * i);
                nodes[i].Add(node);
            }
        }

        for (int i = 1; i < decisionTree.Count; i++)
        {
            for (int j = 0; j < decisionTree[i].Count; j++)
            {
                string way = decisionTree[i][j][1];
                string parent_name = decisionTree[i][j][2];
                if(way != "raiz")
                {
                    GameObject n_link = Instantiate(linkToDraw, canvas.transform);
                    foreach(GameObject gm in nodes[i-1])
                    {
                        if(gm.transform.GetChild(0).GetComponent<Text>().text == parent_name)
                        {
                            n_link.transform.GetChild(0).GetComponent<UILineRenderer>().m_points[0] = new Vector2(gm.transform.position.x-360,gm.transform.position.y-19 - 200);
                            n_link.transform.GetChild(0).GetComponent<UILineRenderer>().m_points[1] = new Vector2(nodes[i][j].transform.position.x-360, nodes[i][j].transform.position.y+ 19- 200);


                            Vector2 p1 = n_link.transform.GetChild(0).GetComponent<UILineRenderer>().m_points[0];
                            Vector2 p2 = n_link.transform.GetChild(0).GetComponent<UILineRenderer>().m_points[1];
                            Vector2 position = (p1 + p2) / 2;
                            position += new Vector2(430, 200);
                            n_link.transform.GetChild(1).position = position;

                            n_link.transform.GetChild(1).GetComponent<Text>().text = way;
                        }
                    }
                }
            }
        }

    }

    private void n_CreateTree(List<string> l_labels,List<List<string>> l_values, int indexToInsert, List<List<string[]>> tree, string link, string comesFrom)
    {
        float count1 = 0.0f;
        float count2 = 0.0f;

        foreach(List<string> value in l_values)
        {
            if(value[value.Count-1] == "True")
            {
                count1 += 1.0f;
            }
            else
            {
                count2 += 2.0f;
            }
        }

        float firstEntropy = Entropy(count1, count2);
        List<float> entropies = new List<float>();

        foreach (string label in l_labels)
        {
            entropies.Add(Entropy(n_MineValuesForLabel(label,l_values)));
        }

        float max = float.NegativeInfinity;
        string labelMax = "INFINITY";
        for (int i = 0; i < entropies.Count - 1; i++) //The last one I don't check it because is the final result
        {
            if (firstEntropy - entropies[i] > max)
            {
                max = firstEntropy - entropies[i];
                labelMax = l_labels[i];
            }
        }

        if (labelMax != "INFINITY")
        {

            List<KeyValuePair<string, float[]>> valoresSelected = n_MineValuesForLabel(labelMax,l_values);

            tree[indexToInsert].Add(new string[3] { labelMax, link, comesFrom });

            foreach (KeyValuePair<string, float[]> results in valoresSelected)
            {
                if (results.Value[0] != 0 && results.Value[1] != 0)
                {
                    List<string> labels_to_new_tree = new List<string>();

                    foreach(string local_label in l_labels)
                    {
                        if (local_label !=  labelMax)
                        {
                            labels_to_new_tree.Add(local_label);
                        }
                    }

                    List<List<string>> values_to_new_tree = new List<List<string>>();

                    foreach(List<string> val in l_values)
                    {
                        if(val[labels.IndexOf(labelMax)] == results.Key)
                        {
                            values_to_new_tree.Add(val);
                        }
                    }

                    n_CreateTree(labels_to_new_tree,values_to_new_tree, indexToInsert + 1, tree, results.Key, labelMax);
                    //Crear nuevo arbol con los valores que coincidan con el string y el valor 
                }
                else
                {
                    if (results.Value[0] != 0)
                    {
                        tree[indexToInsert + 1].Add(new string[3] {"Yes", results.Key, labelMax });
                    }
                    else if (results.Value[1] != 0)
                    {
                        tree[indexToInsert + 1].Add(new string[3] { "No", results.Key, labelMax });
                    }
                }
            }


        }
    }



    private void CreateTree(List<string> l_labels, List<List<string>> l_values, int indexToInsert, List<List<string>> tree)
    {
        float count1 = 0.0f;
        float count2 = 0.0f;

        for(int i=0;i<l_values.Count;i++)
        {
            if(l_values[i][l_values[i].Count-1] == "True")
            {
                count1 += 1.0f;
            }
            else if (l_values[i][l_values[i].Count - 1] == "False")
            {
                count2 += 1.0f;
            }
        }

        float firstEntropy = Entropy(count1, count2); //Hacer funcion que cuente los true y false de la ultim -->Definitivamente 
        List<float> entropies = new List<float>();

        foreach(string label in l_labels)
        {
            entropies.Add(  Entropy(    MineValuesForLabel(label)           ));
        }

        float max = float.NegativeInfinity;
        string labelMax = "INFINITY";
        for(int i=0;i<entropies.Count-1;i++) //The last one I don't check it because is the final result
        {
            if(firstEntropy-entropies[i] > max)
            {
                max = entropies[i];
                labelMax = l_labels[i]; //FALLA POTENCIALMENTE PERO NO LO SE AUN 
            }
        }

        if(labelMax != "INFINITY")
        {

            List<KeyValuePair<string, float[]>> valoresSelected = MineValuesForLabel(labelMax);

            tree[indexToInsert].Add(labelMax);

            List<string> n_labels = new List<string>();
            for(int i=0;i<l_labels.Count;i++)
            {
                if (i != l_labels.IndexOf(labelMax))
                    n_labels.Add(l_labels[i]);
            }

            List<List<string>> n_values = new List<List<string>>();
            for (int i = 0; i < l_values.Count; i++)
            {
                if(i != l_labels.IndexOf(labelMax))
                {
                    n_values.Add(new List<string>());
                    for (int j = 0; j < l_values[i].Count; j++)
                    {
                        n_values[n_values.Count-1].Add(l_values[i][j]);
                    }
                }
            }

            foreach(KeyValuePair<string, float[]> pareja in valoresSelected)
            {

                if (pareja.Value[0] != 0.0f && pareja.Value[1] != 0.0f)
                {
                    CreateTree(n_labels, n_values, indexToInsert + 1, tree);
                }
            }

        }
    }

    private List<KeyValuePair<string,float[]>> MineValuesForLabel(string label)
    {
        List<string> groups = new List<string>();

        List<KeyValuePair<string, float[]>> to_return = new List<KeyValuePair<string, float[]>>();


        for(int i=0;i< values[labels.IndexOf(label)].Count;i++)
        {
            string element = values[labels.IndexOf(label)][i];

            if(!groups.Contains(element))
            {
                groups.Add(element);
                if(values[values.Count-1][i] == "True")
                    to_return.Add(new KeyValuePair<string, float[]>(element, new float[] { 1.0f, 0.0f }));
                else
                    to_return.Add(new KeyValuePair<string, float[]>(element, new float[] { 0.0f, 1.0f }));
            }
            else
            {
                string old_key = to_return[groups.IndexOf(element)].Key;
                float[] old_values = to_return[groups.IndexOf(element)].Value;

                if (values[values.Count - 1][i] == "True")
                    to_return[groups.IndexOf(element)] = new KeyValuePair<string, float[]>(old_key, new float[] { old_values[0]+1.0f, old_values[1] });
                else
                    to_return[groups.IndexOf(element)] = new KeyValuePair<string, float[]>(old_key, new float[] { old_values[0], old_values[1]+1.0f });
            }
        }

        return to_return;
    }

    private List<KeyValuePair<string, float[]>> n_MineValuesForLabel(string label, List<List<string>> l_values)
    {
        List<string> groups = new List<string>();

        List<KeyValuePair<string, float[]>> to_return = new List<KeyValuePair<string, float[]>>();

        int indexOfLabel = labels.IndexOf(label);

        foreach(List<string> value in l_values)
        {
            string element = value[indexOfLabel];

            if (!groups.Contains(element))
            {
                groups.Add(element);
                if (value[value.Count - 1] == "True")
                    to_return.Add(new KeyValuePair<string, float[]>(element, new float[] { 1.0f, 0.0f }));
                else
                    to_return.Add(new KeyValuePair<string, float[]>(element, new float[] { 0.0f, 1.0f }));
            }
            else
            {
                string old_key = to_return[groups.IndexOf(element)].Key;
                float[] old_values = to_return[groups.IndexOf(element)].Value;

                if (value[value.Count-1] == "True")
                    to_return[groups.IndexOf(element)] = new KeyValuePair<string, float[]>(old_key, new float[] { old_values[0] + 1.0f, old_values[1] });
                else
                    to_return[groups.IndexOf(element)] = new KeyValuePair<string, float[]>(old_key, new float[] { old_values[0], old_values[1] + 1.0f });
            }
        }

        return to_return;
    }


    private void ProcessSourceText(string text)
    {
        string aux = "";
        bool inFirstLine = true;
        int inserIndex = 0;

        foreach (char c in text)
        {
            if(c != ':' && c != '\n' && c != '?' && c != '\r')
            {
                aux += c;
            }
            else if(c == '\n' && inFirstLine)
            {
                inFirstLine = false;
                labels.Add(aux);
                values.Add(new List<string>());
                aux = "";
            }
            else if(c == '\n' && !inFirstLine)
            {
                if(aux != "")
                {
                    values[inserIndex].Add(aux);
                    inserIndex = 0;
                    aux = "";
                }
            }
            else if(c == ':')
            {
                if (inFirstLine)
                {
                    if(aux != "")
                    {   
                        labels.Add(aux);
                        values.Add(new List<string>());
                    }
                }
                else
                {
                    if(aux != "")
                    {
                        values[inserIndex].Add(aux);
                        inserIndex++;
                    }
                }

                aux = "";
            }
        }

        foreach(string label in labels) //Peor de los casos - Luego limpiar el arbol si eso 
        {
            decisionTree.Add(new List<string[]>());
        }
    }

    private void n_ProcessSourceText(string text)
    {
        string aux = "";
        bool inFirstLine = true;
        int inserIndex = 0;

        foreach (char c in text)
        {
            if (c != ':' && c != '\n' && c != '?' && c != '\r')
            {
                aux += c;
            }
            else if (c == '\n' && inFirstLine)
            {
                inFirstLine = false;
                labels.Add(aux);
                values.Add(new List<string>());
                aux = "";
            }
            else if (c == '\n' && !inFirstLine)
            {
                if (aux != "")
                {
                    values[inserIndex].Add(aux);
                    values.Add(new List<string>()); 
                    inserIndex++;
                    aux = "";
                }
            }
            else if (c == ':')
            {
                if (inFirstLine)
                {
                    if (aux != "")
                    {
                        labels.Add(aux);
                        //otherWayValues.Add(new List<string>());
                    }
                }
                else
                {
                    if (aux != "")
                    {
                        values[inserIndex].Add(aux);
                    }
                }

                aux = "";
            }
        }

        if (values[values.Count - 1].Count == 0)
        {
            values.Remove(values[values.Count - 1]);
        }

        for(int i=0;i< labels.Count+1 ;i++) //Peor de los casos - Luego limpiar el arbol si eso 
        {
            decisionTree.Add(new List<string[]>());
        }
    }

    private float Entropy(float one,float other)
    {
        if (one == 0 || other == 0) return 0;

        float total = one + other;
        float val1 = one / total;
        float val2 = other / total;
        float log1 = Mathf.Log(val1, 2.0f);
        float log2 = Mathf.Log(val2, 2.0f);

        return -(one / total * log1) - (other / total * log2);
    }
    private float Entropy(List<int> values)
    {
        float total = 0;
        List<float> n_values = new List<float>();


        foreach (float number in values)
        {
            total += number;
        }
        
        for(int i=0;i<values.Count;i++)
        {
            n_values.Add(values[i] / total); //HAS to be exactly 1.0?
        }


        return Entropy(n_values);
    }
    private float Entropy(List<float> values)
    {
        float sum = 0;

        foreach(float number in values)
        {
            sum += -(number * Mathf.Log(number, 2));
        }

        return sum;
    }
    private float Entropy(List<KeyValuePair<string, float[]>> values)
    {
        float sum = 0;
        float total = 0;

        foreach (KeyValuePair<string, float[]> pair in values)
        {
            total += pair.Value[0];
            total += pair.Value[1];
        }

        foreach (KeyValuePair<string, float[]> pair in values)
        {
            float ent = Entropy(pair.Value[0], pair.Value[1]);
            sum = (pair.Value[0] / total) * ent;
        }

        return sum;
    }
    private float InformationGain()
    {
        return 1.0f;
    }


    public List<string> DeepCopy(List<string> toCopy)
    {
        MemoryStream ms = new MemoryStream();
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(ms, toCopy);

        ms.Position = 0;
        return (List<string>)bf.Deserialize(ms);
    }
}
