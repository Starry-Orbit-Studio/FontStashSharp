﻿using FontStashSharp.Interfaces;
using System.Collections.Generic;
using System.Text;
using System;

#if MONOGAME || FNA || XNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#elif STRIDE
using Stride.Core.Mathematics;
using Stride.Graphics;
#else
using System.Drawing;
using System.Numerics;
#endif

namespace FontStashSharp
{
	public abstract partial class SpriteFontBase
	{
		internal static readonly Vector2 DefaultScale = new Vector2(1.0f, 1.0f);
		internal static readonly Vector2 DefaultOrigin = new Vector2(0.0f, 0.0f);

		/// <summary>
		/// Font Size
		/// </summary>
		public int FontSize { get; private set; }

		/// <summary>
		/// Line Height in pixels
		/// </summary>
		public int LineHeight { get; private set; }

		protected float RenderFontSizeMultiplicator { get; set; } = 1f;

		protected SpriteFontBase(int fontSize, int lineHeight)
		{
			FontSize = fontSize;
			LineHeight = lineHeight;
		}

#if MONOGAME || FNA || XNA || STRIDE
		protected internal abstract FontGlyph GetGlyph(GraphicsDevice device, int codepoint);
#else
		protected internal abstract FontGlyph GetGlyph(ITexture2DManager device, int codepoint);
#endif

		protected abstract void PreDraw(string str, out int ascent, out int lineHeight);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="color">A color mask.</param>
		/// <param name="rotation">A rotation of this text in radians.</param>
		/// <param name="origin">Center of the rotation.</param>
		/// <param name="scale">A scaling of this text.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, string text, Vector2 position, Color color,
													Vector2 scale, float rotation, Vector2 origin, float layerDepth = 0.0f)
		{
#if MONOGAME || FNA || XNA || STRIDE
			if (renderer.GraphicsDevice == null)
			{
				throw new ArgumentNullException("renderer.GraphicsDevice can't be null.");
			}
#else
			if (renderer.TextureManager == null)
			{
				throw new ArgumentNullException("renderer.TextureManager can't be null.");
			}
#endif

			if (string.IsNullOrEmpty(text)) return 0.0f;

			scale /= RenderFontSizeMultiplicator;

			int ascent, lineHeight;
			PreDraw(text, out ascent, out lineHeight);

			float originX = 0.0f;
			float originY = 0.0f;

			originY += ascent;

			FontGlyph prevGlyph = null;
			var q = new FontGlyphSquad();
			for (int i = 0; i < text.Length; i += char.IsSurrogatePair(text, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(text, i);
				if (codepoint == '\n')
				{
					originX = 0.0f;
					originY += lineHeight;
					prevGlyph = null;
					continue;
				}

#if MONOGAME || FNA || XNA || STRIDE
				var glyph = GetGlyph(renderer.GraphicsDevice, codepoint);
#else
				var glyph = GetGlyph(renderer.TextureManager, codepoint);
#endif
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, ref originX, originY, ref q);
				if (!glyph.IsEmpty)
				{
					var sourceRect = new Rectangle((int)q.S0, (int)q.T0, (int)(q.S1 - q.S0), (int)(q.T1 - q.T0));

					renderer.Draw(glyph.Texture,
						position,
						sourceRect,
						color,
						rotation,
						origin * (float) RenderFontSizeMultiplicator - q.Offset,
						scale,
						layerDepth);
				}

				prevGlyph = glyph;
			}

			return position.X;
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="color">A color mask.</param>
		/// <param name="scale">A scaling of this text.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, string text, Vector2 position, Color color, Vector2 scale, float layerDepth = 0.0f)
		{
			return DrawText(renderer, text, position, color, scale, 0, DefaultOrigin, layerDepth);
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="color">A color mask.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, string text, Vector2 position, Color color, float layerDepth = 0.0f)
		{
			return DrawText(renderer, text, position, color, DefaultScale, 0, DefaultOrigin, layerDepth);
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="colors">Colors of glyphs.</param>
		/// <param name="rotation">A rotation of this text in radians.</param>
		/// <param name="origin">Center of the rotation.</param>
		/// <param name="scale">A scaling of this text.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, string text, Vector2 position, Color[] colors,
													Vector2 scale, float rotation, Vector2 origin, float layerDepth = 0.0f)
		{
			if (renderer == null)
			{
				throw new ArgumentNullException(nameof(renderer));
			}

#if MONOGAME || FNA || XNA || STRIDE
			if (renderer.GraphicsDevice == null)
			{
				throw new ArgumentNullException("renderer.GraphicsDevice can't be null.");
			}
#else
			if (renderer.TextureManager == null)
			{
				throw new ArgumentNullException("renderer.TextureManager can't be null.");
			}
#endif

			if (string.IsNullOrEmpty(text)) return 0.0f;

			scale /= RenderFontSizeMultiplicator;

			int ascent, lineHeight;
			PreDraw(text, out ascent, out lineHeight);

			float originX = 0.0f;
			float originY = 0.0f;

			originY += ascent;

			FontGlyph prevGlyph = null;
			var pos = 0;
			var q = new FontGlyphSquad();
			for (int i = 0; i < text.Length; i += char.IsSurrogatePair(text, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(text, i);

				if (codepoint == '\n')
				{
					originX = 0.0f;
					originY += lineHeight;
					prevGlyph = null;
					++pos;
					continue;
				}

#if MONOGAME || FNA || XNA || STRIDE
				var glyph = GetGlyph(renderer.GraphicsDevice, codepoint);
#else
				var glyph = GetGlyph(renderer.TextureManager, codepoint);
#endif
				if (glyph == null)
				{
					++pos;
					continue;
				}

				GetQuad(glyph, prevGlyph, ref originX, originY, ref q);
				if (!glyph.IsEmpty)
				{
					var sourceRect = new Rectangle((int)q.S0, (int)q.T0, (int)(q.S1 - q.S0), (int)(q.T1 - q.T0));

					renderer.Draw(glyph.Texture,
						position,
						sourceRect,
						colors[pos],
						rotation,
						origin * (float)RenderFontSizeMultiplicator - q.Offset,
						scale,
						layerDepth);
				}

				prevGlyph = glyph;
				++pos;
			}

			return position.X;
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="colors">Colors of glyphs.</param>
		/// <param name="scale">A scaling of this text.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, string text, Vector2 position, Color[] colors, Vector2 scale, float layerDepth = 0.0f)
		{
			return DrawText(renderer, text, position, colors, scale, 0, DefaultOrigin, layerDepth);
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="colors">Colors of glyphs.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, string text, Vector2 position, Color[] colors, float layerDepth = 0.0f)
		{
			return DrawText(renderer, text, position, colors, DefaultScale, 0, DefaultOrigin, layerDepth);
		}

		protected abstract void PreDraw(StringBuilder str, out int ascent, out int lineHeight);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="color">A color mask.</param>
		/// <param name="rotation">A rotation of this text in radians.</param>
		/// <param name="origin">Center of the rotation.</param>
		/// <param name="scale">A scaling of this text.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, StringBuilder text, Vector2 position, Color color,
													Vector2 scale, float rotation, Vector2 origin, float layerDepth = 0.0f)
		{
			if (renderer == null)
			{
				throw new ArgumentNullException(nameof(renderer));
			}

#if MONOGAME || FNA || XNA || STRIDE
			if (renderer.GraphicsDevice == null)
			{
				throw new ArgumentNullException("renderer.GraphicsDevice can't be null.");
			}
#else
			if (renderer.TextureManager == null)
			{
				throw new ArgumentNullException("renderer.TextureManager can't be null.");
			}
#endif

			if (text == null || text.Length == 0) return 0.0f;

			scale /= RenderFontSizeMultiplicator;

			int ascent, lineHeight;
			PreDraw(text, out ascent, out lineHeight);

			float originX = 0.0f;
			float originY = 0.0f;

			originY += ascent;

			FontGlyph prevGlyph = null;
			var q = new FontGlyphSquad();
			for (int i = 0; i < text.Length; i += StringBuilderIsSurrogatePair(text, i) ? 2 : 1)
			{
				var codepoint = StringBuilderConvertToUtf32(text, i);

				if (codepoint == '\n')
				{
					originX = 0.0f;
					originY += lineHeight;
					prevGlyph = null;
					continue;
				}

#if MONOGAME || FNA || XNA || STRIDE
				var glyph = GetGlyph(renderer.GraphicsDevice, codepoint);
#else
				var glyph = GetGlyph(renderer.TextureManager, codepoint);
#endif
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, ref originX, originY, ref q);
				if (!glyph.IsEmpty)
				{
					var sourceRect = new Rectangle((int)q.S0, (int)q.T0, (int)(q.S1 - q.S0), (int)(q.T1 - q.T0));

					renderer.Draw(glyph.Texture,
						position,
						sourceRect,
						color,
						rotation,
						origin * (float)RenderFontSizeMultiplicator - q.Offset,
						scale,
						layerDepth);
				}

				prevGlyph = glyph;
			}

			return position.X;
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="color">A color mask.</param>
		/// <param name="scale">A scaling of this text.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, StringBuilder text, Vector2 position, Color color, Vector2 scale, float layerDepth = 0.0f)
		{
			return DrawText(renderer, text, position, color, scale, 0, DefaultOrigin, layerDepth);
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="color">A color mask.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, StringBuilder text, Vector2 position, Color color, float layerDepth = 0.0f)
		{
			return DrawText(renderer, text, position, color, DefaultScale, 0, DefaultOrigin, layerDepth);
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="colors">Colors of glyphs.</param>
		/// <param name="rotation">A rotation of this text in radians.</param>
		/// <param name="origin">Center of the rotation.</param>
		/// <param name="scale">A scaling of this text.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, StringBuilder text, Vector2 position, Color[] colors,
													Vector2 scale, float rotation, Vector2 origin, float layerDepth = 0.0f)
		{
			if (renderer == null)
			{
				throw new ArgumentNullException(nameof(renderer));
			}

#if MONOGAME || FNA || XNA || STRIDE
			if (renderer.GraphicsDevice == null)
			{
				throw new ArgumentNullException("renderer.GraphicsDevice can't be null.");
			}
#else
			if (renderer.TextureManager == null)
			{
				throw new ArgumentNullException("renderer.TextureManager can't be null.");
			}
#endif

			if (text == null || text.Length == 0) return 0.0f;

			scale /= RenderFontSizeMultiplicator;

			int ascent, lineHeight;
			PreDraw(text, out ascent, out lineHeight);

			float originX = 0.0f;
			float originY = 0.0f;

			originY += ascent;

			FontGlyph prevGlyph = null;
			var pos = 0;
			var q = new FontGlyphSquad();
			for (int i = 0; i < text.Length; i += StringBuilderIsSurrogatePair(text, i) ? 2 : 1)
			{
				var codepoint = StringBuilderConvertToUtf32(text, i);

				if (codepoint == '\n')
				{
					originX = 0.0f;
					originY += lineHeight;
					prevGlyph = null;
					++pos;
					continue;
				}

#if MONOGAME || FNA || XNA || STRIDE
				var glyph = GetGlyph(renderer.GraphicsDevice, codepoint);
#else
				var glyph = GetGlyph(renderer.TextureManager, codepoint);
#endif
				if (glyph == null)
				{
					++pos;
					continue;
				}

				GetQuad(glyph, prevGlyph, ref originX, originY, ref q);
				if (!glyph.IsEmpty)
				{
					var sourceRect = new Rectangle((int)q.S0, (int)q.T0, (int)(q.S1 - q.S0), (int)(q.T1 - q.T0));

					renderer.Draw(glyph.Texture,
						position,
						sourceRect,
						colors[pos],
						rotation,
						origin * (float)RenderFontSizeMultiplicator - q.Offset,
						scale,
						layerDepth);
				}

				prevGlyph = glyph;
				++pos;
			}

			return position.X;
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="colors">Colors of glyphs.</param>
		/// <param name="scale">A scaling of this text.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, StringBuilder text, Vector2 position, Color[] colors, Vector2 scale, float layerDepth = 0.0f)
		{
			return DrawText(renderer, text, position, colors, scale, 0, DefaultOrigin, layerDepth);
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer.</param>
		/// <param name="text">The text which will be drawn.</param>
		/// <param name="position">The drawing location on screen.</param>
		/// <param name="colors">Colors of glyphs.</param>
		/// <param name="layerDepth">A depth of the layer of this string.</param>
		public float DrawText(IFontStashRenderer renderer, StringBuilder text, Vector2 position, Color[] colors, float layerDepth = 0.0f)
		{
			return DrawText(renderer, text, position, colors, DefaultScale, 0, DefaultOrigin, layerDepth);
		}

		protected virtual void InternalTextBounds(string str, Vector2 position, ref Bounds bounds)
		{
			if (string.IsNullOrEmpty(str)) return;

			int ascent, lineHeight;
			PreDraw(str, out ascent, out lineHeight);

			var x = position.X;
			var y = position.Y;
			var q = new FontGlyphSquad();
			y += ascent;

			float minx, maxx, miny, maxy;
			minx = maxx = x;
			miny = maxy = y;
			float startx = x;

			FontGlyph prevGlyph = null;

			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);
				if (codepoint == '\n')
				{
					x = startx;
					y += lineHeight;
					prevGlyph = null;
					continue;
				}

				var glyph = GetGlyph(null, codepoint);
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, ref x, y, ref q);
				if (q.X0 < minx)
					minx = q.X0;
				if (x > maxx)
					maxx = x;

				if (q.Y0 < miny)
					miny = q.Y0;
				if (q.Y1 > maxy)
					maxy = q.Y1;

				prevGlyph = glyph;
			}

			bounds.X = minx;
			bounds.Y = miny;
			bounds.X2 = maxx;
			bounds.Y2 = maxy;
		}

		public void TextBounds(string str, Vector2 position, ref Bounds bounds, Vector2 scale)
		{
			InternalTextBounds(str, position, ref bounds);
			bounds.ApplyScale(scale / RenderFontSizeMultiplicator);
		}

		public void TextBounds(string str, Vector2 position, ref Bounds bounds)
		{
			TextBounds(str, position, ref bounds, DefaultScale);
		}

		protected virtual void InternalTextBounds(StringBuilder str, Vector2 position, ref Bounds bounds)
		{
			if (str == null || str.Length == 0) return;

			int ascent, lineHeight;
			PreDraw(str, out ascent, out lineHeight);

			var q = new FontGlyphSquad();

			var x = position.X;
			var y = position.Y;
			y += ascent;

			float minx, maxx, miny, maxy;
			minx = maxx = x;
			miny = maxy = y;
			float startx = x;

			FontGlyph prevGlyph = null;

			for (int i = 0; i < str.Length; i += StringBuilderIsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = StringBuilderConvertToUtf32(str, i);

				if (codepoint == '\n')
				{
					x = startx;
					y += lineHeight;
					prevGlyph = null;
					continue;
				}

				var glyph = GetGlyph(null, codepoint);
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, ref x, y, ref q);
				if (q.X0 < minx)
					minx = q.X0;
				if (x > maxx)
					maxx = x;

				if (q.Y0 < miny)
					miny = q.Y0;
				if (q.Y1 > maxy)
					maxy = q.Y1;

				prevGlyph = glyph;
			}

			bounds.X = minx;
			bounds.Y = miny;
			bounds.X2 = maxx;
			bounds.Y2 = maxy;
		}

		public void TextBounds(StringBuilder str, Vector2 position, ref Bounds bounds, Vector2 scale)
		{
			InternalTextBounds(str, position, ref bounds);
			bounds.ApplyScale(scale / RenderFontSizeMultiplicator);
		}

		public void TextBounds(StringBuilder str, Vector2 position, ref Bounds bounds)
		{
			TextBounds(str, position, ref bounds, DefaultScale);
		}

		private static Rectangle ApplyScale(Rectangle rect, Vector2 scale)
		{
			return new Rectangle((int)(rect.X * scale.X),
				(int)(rect.Y * scale.Y),
				(int)(rect.Width * scale.X),
				(int)(rect.Height * scale.Y));
		}

		public List<Rectangle> GetGlyphRects(string str, Vector2 position, Vector2 scale)
		{
			List<Rectangle> Rects = new List<Rectangle>();
			if (string.IsNullOrEmpty(str)) return Rects;

			scale /= RenderFontSizeMultiplicator;

			int ascent, lineHeight;
			PreDraw(str, out ascent, out lineHeight);

			var q = new FontGlyphSquad();

			var x = position.X;
			var y = position.Y;
			y += ascent * scale.Y;

			float startx = x;

			FontGlyph prevGlyph = null;
			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);

				if (codepoint == '\n')
				{
					x = startx;
					y += lineHeight;
					prevGlyph = null;
					continue;
				}

				var glyph = GetGlyph(null, codepoint);
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, ref x, y, ref q);

				var rect = ApplyScale(new Rectangle((int)q.X0, (int)q.Y0, (int)(q.X1 - q.X0), (int)(q.Y1 - q.Y0)), scale);
				Rects.Add(rect);
				prevGlyph = glyph;
			}

			return Rects;
		}

		public List<Rectangle> GetGlyphRects(string str, Vector2 position)
		{
			return GetGlyphRects(str, position, DefaultScale);
		}

		public List<Rectangle> GetGlyphRects(StringBuilder str, Vector2 position, Vector2 scale)
		{
			List<Rectangle> Rects = new List<Rectangle>();
			if (str == null || str.Length == 0) return Rects;

			int ascent, lineHeight;
			PreDraw(str, out ascent, out lineHeight);

			var q = new FontGlyphSquad();

			var x = position.X;
			var y = position.Y;
			y += ascent;

			float minx, maxx, miny, maxy;
			minx = maxx = x;
			miny = maxy = y;
			float startx = x;

			FontGlyph prevGlyph = null;

			for (int i = 0; i < str.Length; i += StringBuilderIsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = StringBuilderConvertToUtf32(str, i);

				if (codepoint == '\n')
				{
					x = startx;
					y += lineHeight;
					prevGlyph = null;
					continue;
				}

				var glyph = GetGlyph(null, codepoint);
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, ref x, y, ref q);

				var rect = ApplyScale(new Rectangle((int)q.X0, (int)q.Y0, (int)(q.X1 - q.X0), (int)(q.Y1 - q.Y0)), scale);
				Rects.Add(rect);
				prevGlyph = glyph;
			}

			return Rects;
		}

		public List<Rectangle> GetGlyphRects(StringBuilder str, Vector2 position)
		{
			return GetGlyphRects(str, position, DefaultScale);
		}

		public Vector2 MeasureString(string text, Vector2 scale)
		{
			Bounds bounds = new Bounds();
			TextBounds(text, Utility.Vector2Zero, ref bounds, scale);

			return new Vector2(bounds.X2, bounds.Y2);
		}

		public Vector2 MeasureString(string text)
		{
			return MeasureString(text, DefaultScale);
		}

		public Vector2 MeasureString(StringBuilder text, Vector2 scale)
		{
			Bounds bounds = new Bounds();
			TextBounds(text, Utility.Vector2Zero, ref bounds, scale);

			return new Vector2(bounds.X2, bounds.Y2);
		}

		public Vector2 MeasureString(StringBuilder text)
		{
			return MeasureString(text, DefaultScale);
		}


		protected static bool StringBuilderIsSurrogatePair(StringBuilder sb, int index)
		{
			if (index + 1 < sb.Length)
				return char.IsSurrogatePair(sb[index], sb[index + 1]);
			return false;
		}

		protected static int StringBuilderConvertToUtf32(StringBuilder sb, int index)
		{
			if (!char.IsHighSurrogate(sb[index]))
				return sb[index];

			return char.ConvertToUtf32(sb[index], sb[index + 1]);
		}

		internal virtual void GetQuad(FontGlyph glyph, FontGlyph prevGlyph, ref float x, float y, ref FontGlyphSquad q)
		{
			float rx = x + glyph.XOffset;
			float ry = y + glyph.YOffset;
			q.X0 = rx;
			q.Y0 = ry;
			q.X1 = rx + glyph.Bounds.Width;
			q.Y1 = ry + glyph.Bounds.Height;
			q.S0 = glyph.Bounds.X;
			q.T0 = glyph.Bounds.Y;
			q.S1 = glyph.Bounds.Right;
			q.T1 = glyph.Bounds.Bottom;
			q.Offset = new Vector2(rx, ry);

			x += glyph.XAdvance;
		}
	}
}