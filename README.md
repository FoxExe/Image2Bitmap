# Image2Bitmap
Image to byte array (bitmap) converter.
Usable for Arduino, STM32, ESP8266, RaspberryPI, OrangePI and other controllers with small LCD screens like PCD8544, SSD1306 and others.

![image2bitmap](https://user-images.githubusercontent.com/3135063/31408341-a0767e98-ae21-11e7-861c-c09119c18a53.jpg)

## [Download windows binary](//github.com/FoxExe/Image2Bitmap/releases/latest)

### Supported conversion formats:
- [X] Monochrome, 8 pixels/byte, Horisontal byte compression
- [X] Monochrome, 8 pixels/byte, Vertical byte compression (PCD8544 / Nokia 3310/5110 LCD)
- [X] Color, RGB332 (8 bit: RRRGGGBB, 256 colors)
- [X] Color, RGB444 (16bit: ----RRRRGGGGBBBB, 4096 colors)
- [X] Color, RGB565 (16bit: RRRRRGGGGGGBBBBB, 65536 colors)

### TODO:
- [X] Two-way conversion (Image-to-byteArray and byteArray-to-Image)
- [X] Zoom/crop preview
- [ ] More formats?
- [ ] Howto, About, Contacts and other info
