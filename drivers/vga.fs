\
\ Fcode payload for QEMU VGA graphics card
\
\ This is the Forth source for an Fcode payload to initialise
\ the QEMU VGA graphics card.
\
\ (C) Copyright 2013 Mark Cave-Ayland
\

fcode-version3

\
\ Dictionary lookups for words that don't have an FCode
\

: (find-xt)   \ ( str len -- xt | -1 )
  $find if
    exit
  else
    -1
  then
;

" openbios-video-addr" (find-xt) cell+ value openbios-video-addr-xt
" openbios-video-width" (find-xt) cell+ value openbios-video-width-xt
" openbios-video-height" (find-xt) cell+ value openbios-video-height-xt
" depth-bits" (find-xt) cell+ value depth-bits-xt
" line-bytes" (find-xt) cell+ value line-bytes-xt

: openbios-video-addr openbios-video-addr-xt @ ;
: openbios-video-width openbios-video-width-xt @ ;
: openbios-video-height openbios-video-height-xt @ ;
: depth-bits depth-bits-xt @ ;
: line-bytes line-bytes-xt @ ;

\
\ IO port words
\

" ioc!" (find-xt) value ioc!-xt
" iow!" (find-xt) value iow!-xt

: ioc! ioc!-xt execute ;
: iow! iow!-xt execute ;

\
\ VGA registers
\

h# 3c0 constant vga-addr
h# 3c8 constant dac-write-addr
h# 3c9 constant dac-data-addr

: vga-color!  ( r g b index -- )
  \ Set the VGA colour registers
  dac-write-addr ioc! rot
  2 >> dac-data-addr ioc! swap
  2 >> dac-data-addr ioc!
  2 >> dac-data-addr ioc!
;

\
\ VBE registers
\

h# 0 constant VBE_DISPI_INDEX_ID
h# 1 constant VBE_DISPI_INDEX_XRES
h# 2 constant VBE_DISPI_INDEX_YRES
h# 3 constant VBE_DISPI_INDEX_BPP
h# 4 constant VBE_DISPI_INDEX_ENABLE
h# 5 constant VBE_DISPI_INDEX_BANK
h# 6 constant VBE_DISPI_INDEX_VIRT_WIDTH
h# 7 constant VBE_DISPI_INDEX_VIRT_HEIGHT
h# 8 constant VBE_DISPI_INDEX_X_OFFSET
h# 9 constant VBE_DISPI_INDEX_Y_OFFSET
h# a constant VBE_DISPI_INDEX_NB

h# 0 constant VBE_DISPI_DISABLED
h# 1 constant VBE_DISPI_ENABLED

\
\ Bochs VBE register writes
\

: vbe-iow!  ( val addr -- )
  h# 1ce iow!
  h# 1d0 iow!
;

\
\ Initialise Bochs VBE mode
\

: vbe-init  ( -- )
  h# 0 vga-addr ioc!    \ Enable blanking
  VBE_DISPI_DISABLED VBE_DISPI_INDEX_ENABLE vbe-iow!
  h# 0 VBE_DISPI_INDEX_X_OFFSET vbe-iow!
  h# 0 VBE_DISPI_INDEX_Y_OFFSET vbe-iow!
  openbios-video-width VBE_DISPI_INDEX_XRES vbe-iow!
  openbios-video-height VBE_DISPI_INDEX_YRES vbe-iow!
  depth-bits VBE_DISPI_INDEX_BPP vbe-iow!
  VBE_DISPI_ENABLED VBE_DISPI_INDEX_ENABLE vbe-iow!
  h# 0 vga-addr ioc!
  h# 20 vga-addr ioc!   \ Disable blanking
;

\
\ Publically visible words
\

external

[IFDEF] CONFIG_MOL
defer mol-color!

\ Hook for MOL (see packages/molvideo.c)

: hw-set-color  ( r g b index -- )
  mol-color!
;

[ELSE]

\ Standard VGA

: hw-set-color  ( r g b index -- )
  vga-color!
;

[THEN]

headerless

\
\ Installation
\

: qemu-vga-driver-install ( -- )
  openbios-video-addr to frame-buffer-adr
  default-font set-font

  frame-buffer-adr encode-int " address" property
  openbios-video-width encode-int " width" property
  openbios-video-height encode-int " height" property
  depth-bits encode-int " depth" property
  line-bytes encode-int " linebytes" property

  openbios-video-width openbios-video-height over char-width / over char-height /
  fb8-install
;

: qemu-vga-driver-init
  vbe-init
  ['] qemu-vga-driver-install is-install
  ;

qemu-vga-driver-init

end0
