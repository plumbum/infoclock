#ifndef _YX32B_H_
#define _YX32B_H_

#include <ch.h>
#include <hal.h>

#define LCD_COLOR(r, g, b) (((((r)>>3) & 0x1F)<<11) \
        | ((((g)>>2) & 0x3F)<<5) \
        | ((((b)>>3) & 0x1F)<<0))

#define COLOR_BLACK 0
#define COLOR_WHITE LCD_COLOR(255, 255, 255)
#define COLOR_RED LCD_COLOR(255, 0, 0)
#define COLOR_GREEN LCD_COLOR(0, 255, 0)
#define COLOR_BLUE LCD_COLOR(0, 0, 255)

typedef uint16_t lcd_data_t;
typedef lcd_data_t lcd_color_t;

int lcdInit(void);
lcd_color_t lcdColor(uint8_t r, uint8_t g, uint8_t b);
void lcdFill(lcd_color_t color);
void lcdPixel(int x, int y, lcd_color_t color);

void lcdChar(int x, int y, char c, lcd_color_t fg, lcd_color_t bg);
void lcdStr(int x, int y, char* s, lcd_color_t fg, lcd_color_t bg);

void lcdTest(void);

#endif /* _YX32B_H_ */
