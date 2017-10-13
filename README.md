# Image2Bitmap
Tool for convert images to different bitmap formats
Usable for Arduino or other controllers.

![image2bitmap](https://user-images.githubusercontent.com/3135063/31408341-a0767e98-ae21-11e7-861c-c09119c18a53.jpg)

### Supported export formats:
- [X] Monochrome, 8 pixels/byte, Horisontal scan
- [X] Monochrome, 8 pixels/byte, Vertical scan (Most Nokia lcd/tft)
- [X] Color, RGB332 (8 bit: RRRGGGBB)
- [X] Color, RGB565 (16bit: RRRRRGGGGGGBBBBB)
- [ ] Color, RGB444 (16bit: ----RRRRGGGGBBBB)

### TODO:
- [X] Two-way conversion (Analyse byte array and convert it to image)
- [ ] Zoom/crop preview
- [ ] More formats?
