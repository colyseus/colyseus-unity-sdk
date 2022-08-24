using System;
using UnityEngine;
using System.Collections;
using UnityEditor;


namespace TMPro.Examples
{
    public class TMP_TextInfoDebugTool : MonoBehaviour
    {
        // Since this script is used for debugging, we exclude it from builds.
        // TODO: Rework this script to make it into an editor utility.
#if UNITY_EDITOR
        public bool ShowCharacters;
        public bool ShowWords;
        public bool ShowLinks;
        public bool ShowLines;
        public bool ShowMeshBounds;
        public bool ShowTextBounds;
        [Space(10)] [TextArea(2, 2)] public string ObjectStats;

        [SerializeField] private TMP_Text m_TextComponent;

        private Transform m_Transform;
        private TMP_TextInfo m_TextInfo;

        private float m_ScaleMultiplier;
        private float m_HandleSize;


        private void OnDrawGizmos()
        {
            if (m_TextComponent == null)
            {
                m_TextComponent = GetComponent<TMP_Text>();

                if (m_TextComponent == null)
                    return;
            }

            m_Transform = m_TextComponent.transform;

            // Get a reference to the text object's textInfo
            m_TextInfo = m_TextComponent.textInfo;

            // Update Text Statistics
            ObjectStats = "Characters: " + m_TextInfo.characterCount + "   Words: " + m_TextInfo.wordCount +
                          "   Spaces: " + m_TextInfo.spaceCount + "   Sprites: " + m_TextInfo.spriteCount +
                          "   Links: " + m_TextInfo.linkCount
                          + "\nLines: " + m_TextInfo.lineCount + "   Pages: " + m_TextInfo.pageCount;

            // Get the handle size for drawing the various
            m_ScaleMultiplier = m_TextComponent.GetType() == typeof(TextMeshPro) ? 1 : 0.1f;
            m_HandleSize = HandleUtility.GetHandleSize(m_Transform.position) * m_ScaleMultiplier;

            // Draw line metrics

            #region Draw Lines

            if (ShowLines)
                DrawLineBounds();

            #endregion

            // Draw word metrics

            #region Draw Words

            if (ShowWords)
                DrawWordBounds();

            #endregion

            // Draw character metrics

            #region Draw Characters

            if (ShowCharacters)
                DrawCharactersBounds();

            #endregion

            // Draw Quads around each of the words

            #region Draw Links

            if (ShowLinks)
                DrawLinkBounds();

            #endregion

            // Draw Quad around the bounds of the text

            #region Draw Bounds

            if (ShowMeshBounds)
                DrawBounds();

            #endregion

            // Draw Quad around the rendered region of the text.

            #region Draw Text Bounds

            if (ShowTextBounds)
                DrawTextBounds();

            #endregion
        }


        /// <summary>
        /// Method to draw a rectangle around each character.
        /// </summary>
        /// <param name="text"></param>
        private void DrawCharactersBounds()
        {
            var characterCount = m_TextInfo.characterCount;

            for (var i = 0; i < characterCount; i++)
            {
                // Draw visible as well as invisible characters
                var characterInfo = m_TextInfo.characterInfo[i];

                var isCharacterVisible = i < m_TextComponent.maxVisibleCharacters &&
                                         characterInfo.lineNumber < m_TextComponent.maxVisibleLines &&
                                         i >= m_TextComponent.firstVisibleCharacter;

                if (m_TextComponent.overflowMode == TextOverflowModes.Page)
                    isCharacterVisible = isCharacterVisible &&
                                         characterInfo.pageNumber + 1 == m_TextComponent.pageToDisplay;

                if (!isCharacterVisible)
                    continue;

                float dottedLineSize = 6;

                // Get Bottom Left and Top Right position of the current character
                var bottomLeft = m_Transform.TransformPoint(characterInfo.bottomLeft);
                var topLeft =
                    m_Transform.TransformPoint(new Vector3(characterInfo.topLeft.x, characterInfo.topLeft.y, 0));
                var topRight = m_Transform.TransformPoint(characterInfo.topRight);
                var bottomRight =
                    m_Transform.TransformPoint(new Vector3(characterInfo.bottomRight.x, characterInfo.bottomRight.y,
                        0));

                // Draw character bounds
                if (characterInfo.isVisible)
                {
                    var color = Color.green;
                    DrawDottedRectangle(bottomLeft, topRight, color);
                }
                else
                {
                    var color = Color.grey;

                    var whiteSpaceAdvance = Math.Abs(characterInfo.origin - characterInfo.xAdvance) > 0.01f
                        ? characterInfo.xAdvance
                        : characterInfo.origin + (characterInfo.ascender - characterInfo.descender) * 0.03f;
                    DrawDottedRectangle(
                        m_Transform.TransformPoint(new Vector3(characterInfo.origin, characterInfo.descender, 0)),
                        m_Transform.TransformPoint(new Vector3(whiteSpaceAdvance, characterInfo.ascender, 0)), color,
                        4);
                }

                var origin = characterInfo.origin;
                var advance = characterInfo.xAdvance;
                var ascentline = characterInfo.ascender;
                var baseline = characterInfo.baseLine;
                var descentline = characterInfo.descender;

                //Draw Ascent line
                var ascentlineStart = m_Transform.TransformPoint(new Vector3(origin, ascentline, 0));
                var ascentlineEnd = m_Transform.TransformPoint(new Vector3(advance, ascentline, 0));

                Handles.color = Color.cyan;
                Handles.DrawDottedLine(ascentlineStart, ascentlineEnd, dottedLineSize);

                // Draw Cap Height & Mean line
                var capline = characterInfo.fontAsset == null
                    ? 0
                    : baseline + characterInfo.fontAsset.faceInfo.capLine * characterInfo.scale;
                var capHeightStart =
                    new Vector3(topLeft.x, m_Transform.TransformPoint(new Vector3(0, capline, 0)).y, 0);
                var capHeightEnd = new Vector3(topRight.x, m_Transform.TransformPoint(new Vector3(0, capline, 0)).y, 0);

                var meanline = characterInfo.fontAsset == null
                    ? 0
                    : baseline + characterInfo.fontAsset.faceInfo.meanLine * characterInfo.scale;
                var meanlineStart =
                    new Vector3(topLeft.x, m_Transform.TransformPoint(new Vector3(0, meanline, 0)).y, 0);
                var meanlineEnd = new Vector3(topRight.x, m_Transform.TransformPoint(new Vector3(0, meanline, 0)).y, 0);

                if (characterInfo.isVisible)
                {
                    // Cap line
                    Handles.color = Color.cyan;
                    Handles.DrawDottedLine(capHeightStart, capHeightEnd, dottedLineSize);

                    // Mean line
                    Handles.color = Color.cyan;
                    Handles.DrawDottedLine(meanlineStart, meanlineEnd, dottedLineSize);
                }

                //Draw Base line
                var baselineStart = m_Transform.TransformPoint(new Vector3(origin, baseline, 0));
                var baselineEnd = m_Transform.TransformPoint(new Vector3(advance, baseline, 0));

                Handles.color = Color.cyan;
                Handles.DrawDottedLine(baselineStart, baselineEnd, dottedLineSize);

                //Draw Descent line
                var descentlineStart = m_Transform.TransformPoint(new Vector3(origin, descentline, 0));
                var descentlineEnd = m_Transform.TransformPoint(new Vector3(advance, descentline, 0));

                Handles.color = Color.cyan;
                Handles.DrawDottedLine(descentlineStart, descentlineEnd, dottedLineSize);

                // Draw Origin
                var originPosition = m_Transform.TransformPoint(new Vector3(origin, baseline, 0));
                DrawCrosshair(originPosition, 0.05f / m_ScaleMultiplier, Color.cyan);

                // Draw Horizontal Advance
                var advancePosition = m_Transform.TransformPoint(new Vector3(advance, baseline, 0));
                DrawSquare(advancePosition, 0.025f / m_ScaleMultiplier, Color.yellow);
                DrawCrosshair(advancePosition, 0.0125f / m_ScaleMultiplier, Color.yellow);

                // Draw text labels for metrics
                if (m_HandleSize < 0.5f)
                {
                    var style = new GUIStyle(GUI.skin.GetStyle("Label"));
                    style.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);
                    style.fontSize = 12;
                    style.fixedWidth = 200;
                    style.fixedHeight = 20;

                    Vector3 labelPosition;
                    var center = (origin + advance) / 2;

                    //float baselineMetrics = 0;
                    //float ascentlineMetrics = ascentline - baseline;
                    //float caplineMetrics = capline - baseline;
                    //float meanlineMetrics = meanline - baseline;
                    //float descentlineMetrics = descentline - baseline;

                    // Ascent Line
                    labelPosition = m_Transform.TransformPoint(new Vector3(center, ascentline, 0));
                    style.alignment = TextAnchor.UpperCenter;
                    Handles.Label(labelPosition, "Ascent Line", style);
                    //Handles.Label(labelPosition, "Ascent Line (" + ascentlineMetrics.ToString("f3") + ")" , style);

                    // Base Line
                    labelPosition = m_Transform.TransformPoint(new Vector3(center, baseline, 0));
                    Handles.Label(labelPosition, "Base Line", style);
                    //Handles.Label(labelPosition, "Base Line (" + baselineMetrics.ToString("f3") + ")" , style);

                    // Descent line
                    labelPosition = m_Transform.TransformPoint(new Vector3(center, descentline, 0));
                    Handles.Label(labelPosition, "Descent Line", style);
                    //Handles.Label(labelPosition, "Descent Line (" + descentlineMetrics.ToString("f3") + ")" , style);

                    if (characterInfo.isVisible)
                    {
                        // Cap Line
                        labelPosition = m_Transform.TransformPoint(new Vector3(center, capline, 0));
                        style.alignment = TextAnchor.UpperCenter;
                        Handles.Label(labelPosition, "Cap Line", style);
                        //Handles.Label(labelPosition, "Cap Line (" + caplineMetrics.ToString("f3") + ")" , style);

                        // Mean Line
                        labelPosition = m_Transform.TransformPoint(new Vector3(center, meanline, 0));
                        style.alignment = TextAnchor.UpperCenter;
                        Handles.Label(labelPosition, "Mean Line", style);
                        //Handles.Label(labelPosition, "Mean Line (" + ascentlineMetrics.ToString("f3") + ")" , style);

                        // Origin
                        labelPosition = m_Transform.TransformPoint(new Vector3(origin, baseline, 0));
                        style.alignment = TextAnchor.UpperRight;
                        Handles.Label(labelPosition, "Origin ", style);

                        // Advance
                        labelPosition = m_Transform.TransformPoint(new Vector3(advance, baseline, 0));
                        style.alignment = TextAnchor.UpperLeft;
                        Handles.Label(labelPosition, "  Advance", style);
                    }
                }
            }
        }


        /// <summary>
        /// Method to draw rectangles around each word of the text.
        /// </summary>
        /// <param name="text"></param>
        private void DrawWordBounds()
        {
            for (var i = 0; i < m_TextInfo.wordCount; i++)
            {
                var wInfo = m_TextInfo.wordInfo[i];

                var isBeginRegion = false;

                var bottomLeft = Vector3.zero;
                var topLeft = Vector3.zero;
                var bottomRight = Vector3.zero;
                var topRight = Vector3.zero;

                var maxAscender = -Mathf.Infinity;
                var minDescender = Mathf.Infinity;

                var wordColor = Color.green;

                // Iterate through each character of the word
                for (var j = 0; j < wInfo.characterCount; j++)
                {
                    var characterIndex = wInfo.firstCharacterIndex + j;
                    var currentCharInfo = m_TextInfo.characterInfo[characterIndex];
                    var currentLine = currentCharInfo.lineNumber;

                    var isCharacterVisible = characterIndex > m_TextComponent.maxVisibleCharacters ||
                                             currentCharInfo.lineNumber > m_TextComponent.maxVisibleLines ||
                                             (m_TextComponent.overflowMode == TextOverflowModes.Page &&
                                              currentCharInfo.pageNumber + 1 != m_TextComponent.pageToDisplay)
                        ? false
                        : true;

                    // Track Max Ascender and Min Descender
                    maxAscender = Mathf.Max(maxAscender, currentCharInfo.ascender);
                    minDescender = Mathf.Min(minDescender, currentCharInfo.descender);

                    if (isBeginRegion == false && isCharacterVisible)
                    {
                        isBeginRegion = true;

                        bottomLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0);
                        topLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0);

                        //Debug.Log("Start Word Region at [" + currentCharInfo.character + "]");

                        // If Word is one character
                        if (wInfo.characterCount == 1)
                        {
                            isBeginRegion = false;

                            topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                            bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                            bottomRight =
                                m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                            topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender,
                                0));

                            // Draw Region
                            DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);

                            //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                        }
                    }

                    // Last Character of Word
                    if (isBeginRegion && j == wInfo.characterCount - 1)
                    {
                        isBeginRegion = false;

                        topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        bottomRight =
                            m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);

                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                    // If Word is split on more than one line.
                    else if (isBeginRegion && currentLine != m_TextInfo.characterInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        bottomRight =
                            m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);
                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                        maxAscender = -Mathf.Infinity;
                        minDescender = Mathf.Infinity;
                    }
                }

                //Debug.Log(wInfo.GetWord(m_TextMeshPro.textInfo.characterInfo));
            }
        }


        /// <summary>
        /// Draw rectangle around each of the links contained in the text.
        /// </summary>
        /// <param name="text"></param>
        private void DrawLinkBounds()
        {
            var textInfo = m_TextComponent.textInfo;

            for (var i = 0; i < textInfo.linkCount; i++)
            {
                var linkInfo = textInfo.linkInfo[i];

                var isBeginRegion = false;

                var bottomLeft = Vector3.zero;
                var topLeft = Vector3.zero;
                var bottomRight = Vector3.zero;
                var topRight = Vector3.zero;

                var maxAscender = -Mathf.Infinity;
                var minDescender = Mathf.Infinity;

                Color32 linkColor = Color.cyan;

                // Iterate through each character of the link text
                for (var j = 0; j < linkInfo.linkTextLength; j++)
                {
                    var characterIndex = linkInfo.linkTextfirstCharacterIndex + j;
                    var currentCharInfo = textInfo.characterInfo[characterIndex];
                    var currentLine = currentCharInfo.lineNumber;

                    var isCharacterVisible = characterIndex > m_TextComponent.maxVisibleCharacters ||
                                             currentCharInfo.lineNumber > m_TextComponent.maxVisibleLines ||
                                             (m_TextComponent.overflowMode == TextOverflowModes.Page &&
                                              currentCharInfo.pageNumber + 1 != m_TextComponent.pageToDisplay)
                        ? false
                        : true;

                    // Track Max Ascender and Min Descender
                    maxAscender = Mathf.Max(maxAscender, currentCharInfo.ascender);
                    minDescender = Mathf.Min(minDescender, currentCharInfo.descender);

                    if (isBeginRegion == false && isCharacterVisible)
                    {
                        isBeginRegion = true;

                        bottomLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0);
                        topLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0);

                        //Debug.Log("Start Word Region at [" + currentCharInfo.character + "]");

                        // If Link is one character
                        if (linkInfo.linkTextLength == 1)
                        {
                            isBeginRegion = false;

                            topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                            bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                            bottomRight =
                                m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                            topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender,
                                0));

                            // Draw Region
                            DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);

                            //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                        }
                    }

                    // Last Character of Link
                    if (isBeginRegion && j == linkInfo.linkTextLength - 1)
                    {
                        isBeginRegion = false;

                        topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        bottomRight =
                            m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);

                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                    // If Link is split on more than one line.
                    else if (isBeginRegion && currentLine != textInfo.characterInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        bottomRight =
                            m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);

                        maxAscender = -Mathf.Infinity;
                        minDescender = Mathf.Infinity;
                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                }

                //Debug.Log(wInfo.GetWord(m_TextMeshPro.textInfo.characterInfo));
            }
        }


        /// <summary>
        /// Draw Rectangles around each lines of the text.
        /// </summary>
        /// <param name="text"></param>
        private void DrawLineBounds()
        {
            var lineCount = m_TextInfo.lineCount;

            for (var i = 0; i < lineCount; i++)
            {
                var lineInfo = m_TextInfo.lineInfo[i];
                var firstCharacterInfo = m_TextInfo.characterInfo[lineInfo.firstCharacterIndex];
                var lastCharacterInfo = m_TextInfo.characterInfo[lineInfo.lastCharacterIndex];

                var isLineVisible = (lineInfo.characterCount == 1 && (firstCharacterInfo.character == 10 ||
                                                                      firstCharacterInfo.character == 11 ||
                                                                      firstCharacterInfo.character == 0x2028 ||
                                                                      firstCharacterInfo.character == 0x2029)) ||
                                    i > m_TextComponent.maxVisibleLines ||
                                    (m_TextComponent.overflowMode == TextOverflowModes.Page &&
                                     firstCharacterInfo.pageNumber + 1 != m_TextComponent.pageToDisplay)
                    ? false
                    : true;

                if (!isLineVisible) continue;

                var lineBottomLeft = firstCharacterInfo.bottomLeft.x;
                var lineTopRight = lastCharacterInfo.topRight.x;

                var ascentline = lineInfo.ascender;
                var baseline = lineInfo.baseline;
                var descentline = lineInfo.descender;

                float dottedLineSize = 12;

                // Draw line extents
                DrawDottedRectangle(m_Transform.TransformPoint(lineInfo.lineExtents.min),
                    m_Transform.TransformPoint(lineInfo.lineExtents.max), Color.green, 4);

                // Draw Ascent line
                var ascentlineStart = m_Transform.TransformPoint(new Vector3(lineBottomLeft, ascentline, 0));
                var ascentlineEnd = m_Transform.TransformPoint(new Vector3(lineTopRight, ascentline, 0));

                Handles.color = Color.yellow;
                Handles.DrawDottedLine(ascentlineStart, ascentlineEnd, dottedLineSize);

                // Draw Base line
                var baseLineStart = m_Transform.TransformPoint(new Vector3(lineBottomLeft, baseline, 0));
                var baseLineEnd = m_Transform.TransformPoint(new Vector3(lineTopRight, baseline, 0));

                Handles.color = Color.yellow;
                Handles.DrawDottedLine(baseLineStart, baseLineEnd, dottedLineSize);

                // Draw Descent line
                var descentLineStart = m_Transform.TransformPoint(new Vector3(lineBottomLeft, descentline, 0));
                var descentLineEnd = m_Transform.TransformPoint(new Vector3(lineTopRight, descentline, 0));

                Handles.color = Color.yellow;
                Handles.DrawDottedLine(descentLineStart, descentLineEnd, dottedLineSize);

                // Draw text labels for metrics
                if (m_HandleSize < 1.0f)
                {
                    var style = new GUIStyle();
                    style.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
                    style.fontSize = 12;
                    style.fixedWidth = 200;
                    style.fixedHeight = 20;
                    Vector3 labelPosition;

                    // Ascent Line
                    labelPosition = m_Transform.TransformPoint(new Vector3(lineBottomLeft, ascentline, 0));
                    style.padding = new RectOffset(0, 10, 0, 5);
                    style.alignment = TextAnchor.MiddleRight;
                    Handles.Label(labelPosition, "Ascent Line", style);

                    // Base Line
                    labelPosition = m_Transform.TransformPoint(new Vector3(lineBottomLeft, baseline, 0));
                    Handles.Label(labelPosition, "Base Line", style);

                    // Descent line
                    labelPosition = m_Transform.TransformPoint(new Vector3(lineBottomLeft, descentline, 0));
                    Handles.Label(labelPosition, "Descent Line", style);
                }
            }
        }


        /// <summary>
        /// Draw Rectangle around the bounds of the text object.
        /// </summary>
        private void DrawBounds()
        {
            var meshBounds = m_TextComponent.bounds;

            // Get Bottom Left and Top Right position of each word
            var bottomLeft = m_TextComponent.transform.position + meshBounds.min;
            var topRight = m_TextComponent.transform.position + meshBounds.max;

            DrawRectangle(bottomLeft, topRight, new Color(1, 0.5f, 0));
        }


        private void DrawTextBounds()
        {
            var textBounds = m_TextComponent.textBounds;

            var bottomLeft = m_TextComponent.transform.position + (textBounds.center - textBounds.extents);
            var topRight = m_TextComponent.transform.position + (textBounds.center + textBounds.extents);

            DrawRectangle(bottomLeft, topRight, new Color(0f, 0.5f, 0.5f));
        }


        // Draw Rectangles
        private void DrawRectangle(Vector3 BL, Vector3 TR, Color color)
        {
            Gizmos.color = color;

            Gizmos.DrawLine(new Vector3(BL.x, BL.y, 0), new Vector3(BL.x, TR.y, 0));
            Gizmos.DrawLine(new Vector3(BL.x, TR.y, 0), new Vector3(TR.x, TR.y, 0));
            Gizmos.DrawLine(new Vector3(TR.x, TR.y, 0), new Vector3(TR.x, BL.y, 0));
            Gizmos.DrawLine(new Vector3(TR.x, BL.y, 0), new Vector3(BL.x, BL.y, 0));
        }

        private void DrawDottedRectangle(Vector3 bottomLeft, Vector3 topRight, Color color, float size = 5.0f)
        {
            Handles.color = color;
            Handles.DrawDottedLine(bottomLeft, new Vector3(bottomLeft.x, topRight.y, bottomLeft.z), size);
            Handles.DrawDottedLine(new Vector3(bottomLeft.x, topRight.y, bottomLeft.z), topRight, size);
            Handles.DrawDottedLine(topRight, new Vector3(topRight.x, bottomLeft.y, bottomLeft.z), size);
            Handles.DrawDottedLine(new Vector3(topRight.x, bottomLeft.y, bottomLeft.z), bottomLeft, size);
        }

        private void DrawSolidRectangle(Vector3 bottomLeft, Vector3 topRight, Color color, float size = 5.0f)
        {
            Handles.color = color;
            var rect = new Rect(bottomLeft, topRight - bottomLeft);
            Handles.DrawSolidRectangleWithOutline(rect, color, Color.black);
        }

        private void DrawSquare(Vector3 position, float size, Color color)
        {
            Handles.color = color;
            var bottomLeft = new Vector3(position.x - size, position.y - size, position.z);
            var topLeft = new Vector3(position.x - size, position.y + size, position.z);
            var topRight = new Vector3(position.x + size, position.y + size, position.z);
            var bottomRight = new Vector3(position.x + size, position.y - size, position.z);

            Handles.DrawLine(bottomLeft, topLeft);
            Handles.DrawLine(topLeft, topRight);
            Handles.DrawLine(topRight, bottomRight);
            Handles.DrawLine(bottomRight, bottomLeft);
        }

        private void DrawCrosshair(Vector3 position, float size, Color color)
        {
            Handles.color = color;

            Handles.DrawLine(new Vector3(position.x - size, position.y, position.z),
                new Vector3(position.x + size, position.y, position.z));
            Handles.DrawLine(new Vector3(position.x, position.y - size, position.z),
                new Vector3(position.x, position.y + size, position.z));
        }


        // Draw Rectangles
        private void DrawRectangle(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Color color)
        {
            Gizmos.color = color;

            Gizmos.DrawLine(bl, tl);
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
        }


        // Draw Rectangles
        private void DrawDottedRectangle(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Color color)
        {
            var cam = Camera.current;
            var dotSpacing = (cam.WorldToScreenPoint(br).x - cam.WorldToScreenPoint(bl).x) / 75f;
            Handles.color = color;

            Handles.DrawDottedLine(bl, tl, dotSpacing);
            Handles.DrawDottedLine(tl, tr, dotSpacing);
            Handles.DrawDottedLine(tr, br, dotSpacing);
            Handles.DrawDottedLine(br, bl, dotSpacing);
        }
#endif
    }
}