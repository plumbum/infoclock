" Для использования этого файла нужно разрешить подключение в ~/.vimrc
" set exrc          " Искать .vimrc в текущей директории
" set secure        " Ограничения на .vimrc в директории

let inc = []
call add(inc, "board")
call add(inc, "../../os/kernel/include")
call add(inc, "../../os/hal/include")
call add(inc, "../../os/hal/platforms/STM32")
call add(inc, "../../os/hal/platforms/STM32/GPIOv1")
call add(inc, "../../os/hal/platforms/STM32/USARTv1")
call add(inc, "../../os/hal/platforms/STM32/USBv1")
call add(inc, "../../os/hal/platforms/STM32F1xx")
call add(inc, "../../os/ports/common/ARMCMx")
call add(inc, "../../os/ports/common/ARMCMx/CMSIS/include")
call add(inc, "../../os/ports/GCC/ARMCMx")
call add(inc, "../../os/ports/GCC/ARMCMx/STM32F1xx")
call add(inc, "../../os/various")

let g:syntastic_c_include_dirs = inc

