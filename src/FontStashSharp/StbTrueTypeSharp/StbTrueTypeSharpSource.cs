﻿using FontStashSharp.Interfaces;
using System;
using static StbTrueTypeSharp.StbTrueType;

namespace FontStashSharp
{
	internal unsafe class StbTrueTypeSharpSource : IFontSource
	{
		private int _ascent, _descent, _lineHeight;
		private readonly Int32Map<int> _kernings = new Int32Map<int>();
		private readonly StbTrueTypeSharpSettings _settings;

		public stbtt_fontinfo _font;

		public StbTrueTypeSharpSource(byte[] data, StbTrueTypeSharpSettings settings)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			_font = CreateFont(data, 0);
			if (_font == null)
				throw new Exception("stbtt_InitFont failed");

			_settings = settings;

			int ascent, descent, lineGap;
			stbtt_GetFontVMetrics(_font, &ascent, &descent, &lineGap);

			_ascent = ascent;
			_descent = descent;
			_lineHeight = ascent - descent + lineGap;
		}

		~StbTrueTypeSharpSource()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && _font != null)
			{
				_font.Dispose();
				_font = null;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private float CalculateScale(int size) => stbtt_ScaleForPixelHeight(_font, size);

		public void GetMetricsForSize(int fontSize, out int ascent, out int descent, out int lineHeight)
		{
			var scale = CalculateScale(fontSize);
			ascent = (int)(_ascent * scale + 0.5f);
			descent = (int)(_descent * scale - 0.5f);
			lineHeight = (int)(_lineHeight * scale + 0.5f);
		}

		public int? GetGlyphId(int codepoint)
		{
			var result = stbtt_FindGlyphIndex(_font, codepoint);
			if (result == 0)
			{
				return null;
			}

			return result;
		}

		public void GetGlyphMetrics(int glyphId, int fontSize, out int advance, out int x0, out int y0, out int x1, out int y1)
		{
			var scale = CalculateScale(fontSize);

			int advanceTemp, lsbTemp;
			stbtt_GetGlyphHMetrics(_font, glyphId, &advanceTemp, &lsbTemp);
			advance = (int)(advanceTemp * scale + 0.5f);

			int x0Temp, y0Temp, x1Temp, y1Temp;
			stbtt_GetGlyphBitmapBox(_font, glyphId, scale, scale, &x0Temp, &y0Temp, &x1Temp, &y1Temp);
			x0 = x0Temp;
			y0 = y0Temp;
			x1 = x1Temp + _settings.KernelWidth;
			y1 = y1Temp + _settings.KernelHeight;
		}

		public void RasterizeGlyphBitmap(int glyphId, int fontSize, byte[] buffer, int startIndex, int outWidth, int outHeight, int outStride)
		{
			var scale = CalculateScale(fontSize);

			fixed (byte* output = &buffer[startIndex])
			{
				stbtt_MakeGlyphBitmap(_font, output, outWidth, outHeight, outStride, scale, scale, glyphId);
				if (_settings.KernelWidth > 0)
					stbtt__v_prefilter(output, outWidth, outHeight, outStride, (uint)_settings.KernelWidth);
				if (_settings.KernelHeight > 0)
					stbtt__h_prefilter(output, outWidth, outHeight, outStride, (uint)_settings.KernelHeight);
			}
		}

		public int GetGlyphKernAdvance(int glyph1, int glyph2, int fontSize)
		{
			var scale = CalculateScale(fontSize);

			var key = ((glyph1 << 16) | (glyph1 >> 16)) ^ glyph2;
			int result;
			if (!_kernings.TryGetValue(key, out result))
			{
				result = stbtt_GetGlyphKernAdvance(_font, glyph1, glyph2);
				_kernings[key] = result;
			}

			return (int)(result * scale);
		}
	}
}