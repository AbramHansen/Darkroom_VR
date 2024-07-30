using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

using BitMiracle.LibTiff.Classic;
using TMPro;
using UnityEngine.WSA;
using static UnityEngine.Rendering.HableCurve;
using static UnityEngine.UI.Image;
using UnityEngine.UIElements;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class ImageScript : MonoBehaviour
{
    private Texture2D texture;

    private Material material;

    float startingViewWidth = 1;

    //dimentions in pixels
    private int width, height;

    private int totalNumPixels;

    private int bitsPerSample;

    private int samplesPerPixel;

    private float widthRatio, heightRatio;

    //unity dimentions
    private float viewWidth, viewHeight;

    Color[] rawData;

    private GameObject frameTop, frameBottom, frameLeft, frameRight;

    private float frameThickness = 0.05f;

    private string filename;

    private string workingDirectory;

    //directory prefabs
    public GameObject folderPrefab;
    public GameObject backButtonPrefab;
    public GameObject tiffFilePrefab;
    public GameObject saveButtonPrefab;

    private bool imageLoaded = false;

    private List<GameObject> folders = new List<GameObject>();
    private List<GameObject> fileList = new List<GameObject>();
    private GameObject backButton;
    private GameObject saveButton;

    //DEBUG VARIABLES
    public bool manuallyUpdateTexture = false;
    public bool triggerPushed = false;

    private Vector2Int coordinatesCenter;
    private Vector2Int coordinatesTop;
    private Vector2Int coordinatesBottom;
    private Vector2Int coordinatesLeft;
    private Vector2Int coordinatesRight;

    //ellipse
    public float hardness;
    public float baseValue;
    private int ellipseWidth;
    private int ellipseHeight;

    private float[] dodgeBurnData;
    private bool dodgeBurnBufferClear = true;
    private float dodgeBurnStrength;

    //input
    private bool rightTriggerValue = false;

    private List<UnityEngine.XR.InputDevice> rightHandDevices = new List<UnityEngine.XR.InputDevice>();
    private UnityEngine.XR.InputDevice device;

    private GameObject light;
    private GameObject tool;

    // Start is called before the first frame update
    void Start()
    {
        width = 1;
        height = 1;

        workingDirectory = @"C:\Users\hanse\Dev\SeniorProject\Unity\DarkroomVR\Assets";

        dodgeBurnStrength = 1f;
        hardness = 0.005f;
        baseValue = 0.25f;

        if (filename == null)
        {
            changeDirectory();
        }

        rawData = new Color[1];
        rawData[0] = Color.gray;

        widthRatio = startingViewWidth;
        heightRatio = (float)width / (float)height;

        viewWidth = startingViewWidth;
        viewHeight = startingViewWidth * ((float)height / (float)width);

        transform.localScale = new Vector3(viewWidth, viewHeight, 1.0f);

        Destroy(frameTop);
        Destroy(frameBottom);
        Destroy(frameLeft);
        Destroy(frameRight);
        createFrame();

        material = GetComponent<MeshRenderer>().material;

        texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        texture.SetPixels(rawData);
        texture.Apply();

        material.SetTexture("_BaseMap", texture);

        //back button and url bar creation
        backButton = Instantiate(backButtonPrefab);

        backButton.transform.parent = this.transform;

        backButton.transform.localPosition = new Vector3(-0.43f, 0.43f, 0.0f);
    }

    void changeDirectory()
    {
        foreach (GameObject folder in folders)
        {
            Destroy(folder);
        }
        foreach (GameObject file in fileList)
        {
            Destroy(file);
        }

        String[] directories = Directory.GetDirectories(workingDirectory);

        Debug.Log(workingDirectory);

        folders = new List<GameObject>();
        fileList = new List<GameObject>();

        int i = 0;
        int j = 1;
        foreach (String dir in directories)
        {
            GameObject folder = Instantiate(folderPrefab);

            folder.transform.parent = this.transform;

            folder.transform.localPosition = new Vector3(-0.43f + (float)i / 8.2f, 0.43f - (float)j / 8.2f, 0.0f);

            TextMeshPro textMeshPro = folder.GetComponentInChildren<TextMeshPro>();
            textMeshPro.text = Path.GetFileName(dir);

            folder.GetComponent<buttonScript>().setDir(dir);

            folders.Add(folder);

            i++;
            if (i == 8)
            {
                i = 0;
                j++;
            }
        }

        String[] files = Directory.GetFiles(workingDirectory);

        foreach (String file in files)
        {
            if (file.EndsWith(".tif") || file.EndsWith(".tiff"))
            {
                GameObject tiffFile = Instantiate(tiffFilePrefab);

                tiffFile.transform.parent = this.transform;

                tiffFile.transform.localPosition = new Vector3(-0.43f + (float)i / 8.2f, 0.43f - (float)j / 8.2f, 0.0f);

                TextMeshPro textMeshPro = tiffFile.GetComponentInChildren<TextMeshPro>();
                textMeshPro.text = Path.GetFileName(file);

                tiffFile.GetComponent<buttonScript>().setDir(file);

                fileList.Add(tiffFile);

                i++;
                if (i == 8)
                {
                    i = 0;
                    j++;
                }
            }
        }
    }

    void loadImage()
    {
        saveButton = Instantiate(saveButtonPrefab);
        saveButton.transform.parent = this.transform;
        saveButton.transform.localPosition = new Vector3(-1 * (widthRatio / 2 + frameThickness / 2),heightRatio / 2 + frameThickness / 2, -1 * (frameThickness - 0.03f));
        saveButton.transform.localScale = new Vector3(frameThickness * widthRatio, frameThickness * heightRatio, 0.01f);

        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightHandDevices);
        device = rightHandDevices[0];

        foreach (GameObject folder in folders)
        {
            Destroy(folder);
        }
        foreach (GameObject file in fileList)
        {
            Destroy(file);
        }

        Destroy(backButton);

        Debug.Log(workingDirectory);

        // Open the TIFF image
        using (Tiff image = Tiff.Open(workingDirectory, "r"))
        {
            if (image == null)
            {
                return;
            }

            FieldValue[] value;
            value = image.GetField(TiffTag.IMAGEWIDTH);
            width = value[0].ToInt();

            value = image.GetField(TiffTag.IMAGELENGTH);
            height = value[0].ToInt();

            value = image.GetField(TiffTag.BITSPERSAMPLE);
            bitsPerSample = value[0].ToInt();

            value = image.GetField(TiffTag.SAMPLESPERPIXEL);
            samplesPerPixel = value[0].ToInt();

            totalNumPixels = width * height;
            int bytesPerSample = bitsPerSample / 8;
            int bytesPerPixel = bytesPerSample * samplesPerPixel;

            Debug.Log("Width: " + width);
            Debug.Log("Height: " + height);
            Debug.Log("Bit Depth: " + bitsPerSample);
            Debug.Log("Bytes Per Sample: " + bytesPerSample);
            Debug.Log("Bytes Per Pixel: " + bytesPerPixel);

            rawData = new Color[width * height];
            dodgeBurnData = new float[width * height];

            initDodgeBurnData();

            byte[] buf = new byte[image.StripSize()];

            int rawDataIndex = 0;
            for (int strip = 0; strip < image.NumberOfStrips(); strip++)
            {
                image.ReadEncodedStrip(strip, buf, 0, -1);

                for (int byteIndex = 0; byteIndex < image.StripSize(); byteIndex += bytesPerPixel)
                {
                    if (samplesPerPixel == 1) //grayscale
                    {
                        if (bytesPerSample == 2)
                        {
                            ushort pixel = BitConverter.ToUInt16(buf, byteIndex);

                            if (rawDataIndex < totalNumPixels)
                            {
                                rawData[rawDataIndex] = new Color(
                                    (float)pixel / 65535f,
                                    (float)pixel / 65535f,
                                    (float)pixel / 65535f);
                                rawDataIndex++;
                            }
                        }
                        else
                        {
                            byte pixel = buf[byteIndex];

                            if (rawDataIndex < totalNumPixels)
                            {
                                rawData[rawDataIndex] = new Color(
                                    (float)pixel / 255f,
                                    (float)pixel / 255f,
                                    (float)pixel / 255f);
                                rawDataIndex++;
                            }
                        }
                    }
                    else //color
                    {
                        if(bytesPerSample == 2)
                        {
                            ushort red = BitConverter.ToUInt16(buf, byteIndex);
                            ushort green = BitConverter.ToUInt16(buf, byteIndex + 2);
                            ushort blue = BitConverter.ToUInt16(buf, byteIndex + 4);

                            if (rawDataIndex < totalNumPixels)
                            {
                                rawData[rawDataIndex] = new Color(
                                    (float)red / 65535f,
                                    (float)green / 65535f,
                                    (float)blue / 65535f);
                                rawDataIndex++;
                            }
                        }
                        else
                        {
                            byte red = buf[byteIndex];
                            byte green = buf[byteIndex+1];
                            byte blue = buf[byteIndex+2];

                            if (rawDataIndex < totalNumPixels)
                            {
                                rawData[rawDataIndex] = new Color(
                                    (float)red / 255f,
                                    (float)green / 255f,
                                    (float)blue / 255f);
                                rawDataIndex++;
                            }
                        }
                    }
                }
            }

            flipVertically();

            widthRatio = startingViewWidth;
            heightRatio = (float)width / (float)height;

            viewWidth = startingViewWidth;
            viewHeight = startingViewWidth * ((float)height / (float)width);

            transform.localScale = new Vector3(viewWidth, viewHeight, 1.0f);

            createFrame();

            material = GetComponent<MeshRenderer>().material;

            texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            texture.SetPixels(rawData);
            texture.Apply();

            material.SetTexture("_BaseMap", texture);
        }
    }

    void createFrame()
    {
        // Create frame top
        frameTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frameTop.transform.parent = this.transform;
        frameTop.transform.localPosition = new Vector3(0.0f, 0.5f + frameThickness * heightRatio / 2, 0.0f);
        frameTop.transform.localScale = new Vector3(1.0f + frameThickness * widthRatio * 2, frameThickness * heightRatio, frameThickness);

        // Create frame bottom
        frameBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frameBottom.transform.parent = this.transform;
        frameBottom.transform.localPosition = new Vector3(0.0f,-(0.5f + frameThickness * heightRatio / 2), 0.0f);
        frameBottom.transform.localScale = new Vector3(1.0f + frameThickness * widthRatio * 2, frameThickness * heightRatio, frameThickness);

        // Create frame left
        frameLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frameLeft.transform.parent = this.transform;
        frameLeft.transform.localPosition = new Vector3(-(0.5f + frameThickness * widthRatio / 2), 0.0f, 0.0f);
        frameLeft.transform.localScale = new Vector3(frameThickness * widthRatio, 1.0f + frameThickness * heightRatio * 2, frameThickness);

        // Create frame right
        frameRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frameRight.transform.parent = this.transform;
        frameRight.transform.localPosition = new Vector3(0.5f + frameThickness * widthRatio / 2, 0.0f, 0.0f);
        frameRight.transform.localScale = new Vector3(frameThickness * widthRatio, 1.0f + frameThickness * heightRatio * 2, frameThickness);
    }

    // Update is called once per frame
    void Update()
    {
        if (manuallyUpdateTexture)
        {
            texture.SetPixels(rawData);
            texture.Apply();

            material.SetTexture("_BaseMap", texture);

            manuallyUpdateTexture = false;
        }

        calculateRays();

        if (!imageLoaded)
        {
            foreach (GameObject folder in folders)
            {
                if (folder.GetComponent<buttonScript>().hasBeenPressed())
                {
                    workingDirectory = folder.GetComponent<buttonScript>().getDir();

                    changeDirectory();
                    return;
                }
            }
            foreach (GameObject file in fileList)
            {
                if (file.GetComponent<buttonScript>().hasBeenPressed())
                {
                    imageLoaded = true;
                    workingDirectory = file.GetComponent<buttonScript>().getDir();

                    loadImage();
                    return;
                }
            }
            if (backButton.GetComponent<buttonScript>().hasBeenPressed())
            {
                Debug.Log("BACK BUTTON PRESSED");

                DirectoryInfo directoryInfo = new DirectoryInfo(workingDirectory);
                DirectoryInfo parentDirectory = directoryInfo.Parent;
                if (parentDirectory != null)
                {
                    workingDirectory = parentDirectory.FullName;
                }
                changeDirectory();
                backButton.GetComponent<buttonScript>().reset();
            }
        }
        else
        {
            updateDodgeBurnData();

            if (saveButton.GetComponent<buttonScript>().hasBeenPressed())
            {
                saveImage();
            }
        }
        updateTool();
    }

    void calculateRays()
    {
        tool = GameObject.Find("Tool");
        light = GameObject.Find("Main Light");

        Vector3 lightPosition = light.transform.position;
        Vector3 toolCenter = tool.transform.position;

        Vector3 toolTopPoint = tool.transform.TransformPoint(new Vector3(0.0f, 0.0f, 0.5f));
        Vector3 toolBottomPoint = tool.transform.TransformPoint(new Vector3(0.0f, 0.0f, -0.5f));
        Vector3 toolLeftPoint = tool.transform.TransformPoint(new Vector3(-0.5f, 0.0f, 0.0f));
        Vector3 toolRightPoint = tool.transform.TransformPoint(new Vector3(0.5f, 0.0f, 0.0f));

        Vector3 center = calculateRay(lightPosition, toolCenter);

        Vector3 top = calculateRay(lightPosition, toolTopPoint);
        Vector3 bottom = calculateRay(lightPosition, toolBottomPoint);
        Vector3 left = calculateRay(lightPosition, toolLeftPoint);
        Vector3 right = calculateRay(lightPosition, toolRightPoint);

        Debug.DrawLine(center, top);
        Debug.DrawLine(center, bottom);
        Debug.DrawLine(center, left);
        Debug.DrawLine(center, right);

        Vector3 localCenter = transform.InverseTransformPoint(center);
        Vector3 localTop = transform.InverseTransformPoint(top);
        Vector3 localBottom = transform.InverseTransformPoint(bottom);
        Vector3 localLeft = transform.InverseTransformPoint(left);
        Vector3 localRight = transform.InverseTransformPoint(right);

        //get rotation based off of left and right points
        float leftRightHeight = localRight.y - localLeft.y;
        float leftRightWidth = localRight.x - localLeft.x;

        double rotation = 0;
        if(leftRightWidth != 0)
        {
            rotation = Math.Atan(leftRightHeight / leftRightWidth);
        }

        //get skew based off of angle bwtween 

        coordinatesCenter = convertLocalToImageCoordinates(localCenter);
        coordinatesTop = convertLocalToImageCoordinates(localTop);
        coordinatesBottom = convertLocalToImageCoordinates(localBottom);
        coordinatesLeft = convertLocalToImageCoordinates(localLeft);
        coordinatesRight = convertLocalToImageCoordinates(localRight);
    }

    Vector3 calculateRay(Vector3 source, Vector3 directionPoint)
    {
        Vector3 hitPoint = new Vector3();

        Vector3 directionVector = directionPoint - source;

        directionVector = directionVector.normalized;

        Ray centerRay = new Ray(source, directionVector);

        Plane plane = new Plane(getNormal(), transform.position);

        float enter = 0.0f;
        if (plane.Raycast(centerRay, out enter))
        {
            hitPoint = centerRay.GetPoint(enter);
        }

        Debug.DrawRay(centerRay.origin, centerRay.direction * 10, Color.blue, 0.1f, true);

        return hitPoint;
    }

    Vector3 getNormal()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;

        Vector3 v0 = vertices[0];
        Vector3 v1 = vertices[1];
        Vector3 v2 = vertices[2];

        // Calculate two edge vectors
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;

        // Calculate the normal using the cross product
        Vector3 normal = Vector3.Cross(edge1, edge2).normalized;

        // Transform the normal to world space
        normal = transform.TransformDirection(normal);

        return normal;
    }

    Vector2Int convertLocalToImageCoordinates(Vector3 local)
    {
        Vector3 topCornerLocal = local + new Vector3(0.5f, -0.5f, 0.0f);
        topCornerLocal.y *= -1;

        int x = Mathf.RoundToInt((float)width * topCornerLocal.x);
        int y = Mathf.RoundToInt((float)height * topCornerLocal.y);

        return new Vector2Int(x, y);
    }

    private void SetPixel(Vector2Int position, Color color)
    {
        // Ensure the position is within the bounds of the image
        if (position.x < 0 || position.x >= width || position.y < 0 || position.y >= height)
        {
            Debug.LogError("Position is out of bounds");
            return;
        }

        // Invert the y-coordinate to match the top-left origin
        int invertedY = height - 1 - position.y;

        // Calculate the index in the one-dimensional array
        int index = invertedY * width + position.x;

        // Set the color at the specified index
        rawData[index] = color;
    }

    private void AddDodgeBurnPixel(Vector2Int position, float intensity)
    {
        // Ensure the position is within the bounds of the image
        if (position.x < 0 || position.x >= width || position.y < 0 || position.y >= height)
        {
            Debug.LogError("Position is out of bounds");
            return;
        }

        // Invert the y-coordinate to match the top-left origin
        int invertedY = height - 1 - position.y;

        // Calculate the index in the one-dimensional array
        int index = invertedY * width + position.x;

        // Set the intensity at the specified index
        dodgeBurnData[index] += 1.0f - intensity;
    }

    private void flipVertically()
    {
        for (int y = 0; y < height / 2; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculate the index of the current pixel and its vertically flipped counterpart
                int topIndex = y * width + x;
                int bottomIndex = (height - 1 - y) * width + x;

                // Swap the pixels
                Color temp = rawData[topIndex];
                rawData[topIndex] = rawData[bottomIndex];
                rawData[bottomIndex] = temp;
            }
        }
    }

    private void updateTool()
    {
        Vector2 joystickValue;
        device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out joystickValue);

        if (joystickValue.x > 0.5f)
        {
            // Joystick moved to the right
            tool.GetComponent<ToolScript>().increaseScale();
        }
        else if (joystickValue.x < -0.5f)
        {
            // Joystick moved to the left
            tool.GetComponent<ToolScript>().decreaseScale();
        }

        if (joystickValue.y > 0.5f)
        {
            // Joystick moved up
            dodgeBurnStrength += 0.01f;

            if (dodgeBurnStrength > 1)
            {
                dodgeBurnStrength = 1;
            }
        }
        else if (joystickValue.y < -0.5f)
        {
            // Joystick moved down
            dodgeBurnStrength -= 0.01f;

            if (dodgeBurnStrength < -1)
            {
                dodgeBurnStrength = -1;
            }
        }

        float mappedStrength = (float)Map((double)dodgeBurnStrength, -1f, 1f, 0f, 1f);
        tool.GetComponent<MeshRenderer>().material.color = new Color(mappedStrength, mappedStrength, mappedStrength, 1.0f);
    }

    private void updateDodgeBurnData()
    {
        bool triggerValue;
        if ((device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerValue) && triggerValue) || triggerPushed)
        {
            Debug.Log("Trigger button is pressed.");

            drawEllipse();

            dodgeBurnBufferClear = false;
        }
        else
        {
            if(!dodgeBurnBufferClear)
            {
                for(int i = 0; i < width * height; i++)
                {
                    float red = rawData[i].r;
                    float green = rawData[i].g;
                    float blue = rawData[i].b;

                    //A dodgeBurnStrength of 1 = full strength dodge. -1 is full strength burn.
                    red += dodgeBurnData[i] * dodgeBurnStrength;
                    green += dodgeBurnData[i] * dodgeBurnStrength;
                    blue += dodgeBurnData[i] * dodgeBurnStrength;

                    if(red < 0) red = 0;
                    if(green < 0) green = 0;
                    if(blue < 0) blue = 0;

                    rawData[i] = new Color(red, green, blue);
                }
                dodgeBurnData = new float[width * height];
                initDodgeBurnData();

                material = GetComponent<MeshRenderer>().material;

                texture = new Texture2D(width, height);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                texture.SetPixels(rawData);
                texture.Apply();

                material.SetTexture("_BaseMap", texture);

                dodgeBurnBufferClear = true;

                Debug.Log("CLEARED!");
            }
        }      
    }

    private void drawEllipse()
    {
        ellipseWidth = (int)GetDistance(coordinatesRight, coordinatesLeft) / 2;
        ellipseHeight = (int)GetDistance(coordinatesTop, coordinatesBottom) / 2;

        int horizontalShearAmount = 0;

        if (ellipseWidth == 0 || ellipseHeight == 0)
        {
            return;
        }

        var currentHorizontalOffset = 0;

        Debug.Log("EWIDTH " + ellipseWidth);
        Debug.Log("EHEIGHT" + ellipseHeight);

        for (int y = -ellipseHeight; y <= ellipseHeight; y++)
        {
            for (int x = -ellipseWidth; x <= ellipseWidth; x++)
            {
                if (x * x * ellipseHeight * ellipseHeight + y * y * ellipseWidth * ellipseWidth <= ellipseHeight * ellipseHeight * ellipseWidth * ellipseWidth)
                {
                    float normalizedX = x / (float)ellipseWidth;
                    float normalizedY = y / (float)ellipseHeight;

                    double distance = Math.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);

                    double distanceSquared = distance + 0.01 * distance + 0.01;

                    double value = Math.Pow(distanceSquared, hardness);
                    value = Map(value, 0.0f, 1.0f, baseValue, 1.0f);

                    value = Math.Clamp(value, 0.0f, 1.0f);

                    AddDodgeBurnPixel(new Vector2Int(coordinatesCenter.x + x + currentHorizontalOffset, coordinatesCenter.y + y), (float)value);
                }
            }
            currentHorizontalOffset += horizontalShearAmount;
        }
    }

    public static double Map(double value, float fromLow, float fromHigh, float toLow, float toHigh)
    {
        return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
    }

    public static float GetDistance(Vector2Int point1, Vector2Int point2)
    {
        float dx = point2.x - point1.x;
        float dy = point2.y - point1.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private void initDodgeBurnData()
    {
        for (int i = 0; i < width * height; i++)
        {
            dodgeBurnData[i] = 0.0f;
        }
    }

    private void saveImage()
    {
        saveButton.GetComponent<buttonScript>().reset();

        flipVertically();

        Debug.Log("SAVING!");

        string fileName = @"C:\Users\hanse\Dev\SeniorProject\Unity\DarkroomVR\Assets\output.tiff";

        using (Tiff output = Tiff.Open(fileName, "w"))
        {
            output.SetField(TiffTag.IMAGEWIDTH, width);
            output.SetField(TiffTag.IMAGELENGTH, height);
            output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
            output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample);
            output.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
            output.SetField(TiffTag.ROWSPERSTRIP, height);
            output.SetField(TiffTag.XRESOLUTION, 88.0);
            output.SetField(TiffTag.YRESOLUTION, 88.0);
            output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);
            output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
            output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
            output.SetField(TiffTag.COMPRESSION, Compression.NONE);
            output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

            int rawDataIndex = 0;
            int bytesPerSample = samplesPerPixel * bitsPerSample / 8;
            byte[] buf = new byte[width * bytesPerSample];

            for (int scanline = 0; scanline < height; scanline++)
            {
                for (int byteIndex = 0; byteIndex < width * bytesPerSample; byteIndex += bytesPerSample)
                {
                    if (samplesPerPixel == 1) // grayscale
                    {
                        if (bytesPerSample == 2)
                        {
                            ushort pixelValue = (ushort)Math.Clamp(rawData[rawDataIndex].r * 65535f, 0f, 65535f);
                            BitConverter.GetBytes(pixelValue).CopyTo(buf, byteIndex);
                        }
                        else
                        {
                            byte pixelValue = (byte)Math.Clamp(rawData[rawDataIndex].r * 255f, 0f, 255f);
                            buf[byteIndex] = pixelValue;
                        }
                        rawDataIndex++;
                    }
                    else // color
                    {
                        if (bytesPerSample == 2)
                        {
                            ushort redValue = (ushort)Math.Clamp(rawData[rawDataIndex].r * 65535f, 0f, 65535f);
                            ushort greenValue = (ushort)Math.Clamp(rawData[rawDataIndex].g * 65535f, 0f, 65535f);
                            ushort blueValue = (ushort)Math.Clamp(rawData[rawDataIndex].b * 65535f, 0f, 65535f);

                            BitConverter.GetBytes(redValue).CopyTo(buf, byteIndex);
                            BitConverter.GetBytes(greenValue).CopyTo(buf, byteIndex + 2);
                            BitConverter.GetBytes(blueValue).CopyTo(buf, byteIndex + 4);
                        }
                        else
                        {
                            byte redValue = (byte)Math.Clamp(rawData[rawDataIndex].r * 255f, 0f, 255f);
                            byte greenValue = (byte)Math.Clamp(rawData[rawDataIndex].g * 255f, 0f, 255f);
                            byte blueValue = (byte)Math.Clamp(rawData[rawDataIndex].b * 255f, 0f, 255f);

                            buf[byteIndex] = redValue;
                            buf[byteIndex + 1] = greenValue;
                            buf[byteIndex + 2] = blueValue;
                        }
                        rawDataIndex++;
                    }
                }

                // Write the scanline buffer to the output
                output.WriteScanline(buf, scanline);
            }
        }

        System.Diagnostics.Process.Start(fileName);

        filename = null;
        imageLoaded = false;

        Destroy(saveButton);

        Start();
    }
}