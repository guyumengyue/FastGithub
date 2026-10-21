---
name: FastGithub
description: A calm native Windows control panel for local GitHub acceleration.
colors:
  accent: "#245FC4"
  accent-soft: "#E1EBFC"
  accent-hover: "#174DA8"
  accent-pressed: "#123C84"
  canvas: "#F4F6FA"
  panel: "#FFFFFF"
  rail: "#EDF1F7"
  ink: "#202B3D"
  muted: "#5D6B80"
  line: "#DCE2EB"
  secondary-pressed: "#D3E1F7"
  navigation-hover: "#E3E9F2"
  state-ready: "#24734A"
  state-pending: "#8A6500"
  state-error: "#B43D35"
  state-stopped: "#79869A"
  chart-upload: "#7C8BA4"
typography:
  headline:
    fontFamily: "Segoe UI, Microsoft YaHei UI"
    fontSize: "26px"
    fontWeight: 600
  service-state:
    fontFamily: "Segoe UI, Microsoft YaHei UI"
    fontSize: "21px"
    fontWeight: 600
  title:
    fontFamily: "Segoe UI, Microsoft YaHei UI"
    fontSize: "18px"
    fontWeight: 600
  body:
    fontFamily: "Segoe UI, Microsoft YaHei UI"
    fontSize: "14px"
    fontWeight: 400
  secondary:
    fontFamily: "Segoe UI, Microsoft YaHei UI"
    fontSize: "13px"
    fontWeight: 400
  caption:
    fontFamily: "Segoe UI, Microsoft YaHei UI"
    fontSize: "11px"
    fontWeight: 400
  rate:
    fontFamily: "Consolas"
    fontSize: "26px"
    fontWeight: 600
  timestamp:
    fontFamily: "Consolas"
    fontSize: "12px"
    fontWeight: 400
rounded:
  badge: "4px"
  control: "6px"
  panel: "12px"
spacing:
  small: "8px"
  control-gap: "10px"
  medium: "12px"
  section-gap: "16px"
  panel-gap: "20px"
  panel-inset: "22px"
  page-inline: "28px"
components:
  button-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.panel}"
    rounded: "{rounded.control}"
    padding: "10px 22px"
  button-primary-hover:
    backgroundColor: "{colors.accent-hover}"
  button-primary-pressed:
    backgroundColor: "{colors.accent-pressed}"
  button-secondary:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    padding: "8px 14px"
  button-secondary-hover:
    backgroundColor: "{colors.accent-soft}"
    textColor: "{colors.accent}"
  input:
    textColor: "{colors.ink}"
    padding: "8px 10px"
    typography: "{typography.secondary}"
  navigation:
    textColor: "{colors.muted}"
    rounded: "{rounded.control}"
    padding: "13px 16px"
    typography: "{typography.body}"
  navigation-selected:
    backgroundColor: "{colors.accent-soft}"
    textColor: "{colors.accent}"
  mode-badge:
    backgroundColor: "{colors.accent-soft}"
    textColor: "{colors.accent}"
    rounded: "{rounded.badge}"
    padding: "3px 8px"
  panel:
    backgroundColor: "{colors.panel}"
    rounded: "{rounded.panel}"
    padding: "22px"
---

# Design System: FastGithub

## Overview

**Creative North Star: "The Local Service Console"**

FastGithub is a native Windows WPF application on .NET Framework 4.5. Its interface adopts the familiar organization of a desktop proxy client: persistent navigation, a prominent service control, and focused work areas for traffic, diagnostics, connection settings, and help. The existing light gray-blue identity is expressed through quiet surfaces and a restrained blue accent.

The visual priority is operational clarity. The service state and next action remain visible across pages; metadata and troubleshooting text sit below the main task. Text, color, and control availability communicate the actual local service lifecycle. The name above describes the implemented interface, rather than a separate brand identity exercise.

**Key Characteristics:**
- Native Windows window chrome and desktop controls.
- A fixed navigation rail beside a flexible content area.
- White panels on a cool, pale canvas with minimal visual depth.
- Blue actions, clear Chinese labels, and monospaced measurements.
- Explicit empty, pending, stopped, ready, and error states.

The implementation sources are `FastGithub.UI/Theme.xaml`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `FlowChart.xaml(.cs)`, `UdpLogListBox.xaml(.cs)`, and the native `IssuesWebbrowser.xaml` help control. Reference captures are in `artifacts/ui/`.

## Colors

The palette pairs a clear service blue with cool neutrals; green, amber, and red carry operational meaning.

### Primary

- **Service Blue** (`accent`): primary service action, selected navigation, download rate, and keyboard focus.
- **Pale Service Blue** (`accent-soft`): selected navigation and mode badges; secondary-button hover surface.
- **Deep Service Blue** (`accent-hover`, `accent-pressed`): progressively darker primary-button interaction states.

### Neutral

- **Cool Canvas** (`canvas`): main window and inset address surface.
- **White Panel** (`panel`): principal content surfaces and button text on blue.
- **Blue-Gray Rail** (`rail`): continuous navigation background.
- **Deep Ink** (`ink`): primary text and upload measurements.
- **Slate Text** (`muted`): secondary copy, metadata, and inactive navigation.
- **Quiet Divider** (`line`): navigation separator, service-card outline, and control borders.
- **Pressed Mist** (`secondary-pressed`) and **Rail Hover** (`navigation-hover`): local interaction feedback.
- **Upload Slate** (`chart-upload`): dashed upload series, visually distinct from the blue download series.

Service colors are semantic: `state-ready` means the local core responded, `state-pending` means a transition or unavailable readiness, `state-error` means an operation failed or the core exited, and `state-stopped` means stopped. They are not additional decorative accents.

**The State Has Words Rule.** Pair every service-state color with its visible state label and explanation. Local readiness must not be presented as proof of GitHub reachability.

## Typography

The inherited interface font is **Segoe UI, Microsoft YaHei UI**, retaining Windows and Chinese text support. **Consolas** distinguishes rates, timestamps, and the local proxy address. No custom font is loaded.

The frontmatter expresses WPF font sizes as `px` solely for the portable token schema. In the application, these numbers are device-independent pixels (DIP), and `SemiBold` corresponds to weight 600. Font fallback and native text rendering remain Windows responsibilities.

### Hierarchy

- **Headline:** the page title.
- **Service state:** the persistent status label.
- **Title:** section headings such as connection mode and real-time traffic.
- **Body:** navigation and standard control copy.
- **Secondary:** explanatory text and most button labels.
- **Caption:** mode badges, record counts, footer notes, and log metadata.
- **Rate / timestamp:** monospaced numeric roles defined in the frontmatter.

Log messages use the secondary size with an explicit line height (21 DIP); native help paragraphs use a line height (23 DIP). Other text retains native line metrics. The rail brand label uses a semibold size (23 DIP) without establishing a separate display typeface.

## Layout

The initial window is 980 by 720 DIP, with a minimum of 860 by 620 DIP. Preserve native window chrome, resizing, layout rounding, and device-pixel snapping. The left navigation rail is fixed (174 DIP); the right column fills the remaining width.

Content uses horizontal page margins from `page-inline`, a top inset (24 DIP), and a bottom inset (18 DIP). The page heading precedes a shared service card, followed by a page-specific work area and a small feedback line. The main content row stretches; settings and help scroll vertically when needed. The rail remains visible at the minimum size. There are no implemented mobile layouts or breakpoint-driven navigation changes.

Panels generally use `panel-inset`. The service card uses vertical padding (20 DIP), the log panel uses padding (18 DIP), and spacing is selected from the observed values rather than forced onto an invented uniform grid. The service action aligns right; its minimum width is 112 DIP. The page-level GitHub action also aligns right.

## Elevation & Depth

The application uses flat tonal layering. White panels separate from the cool canvas; the rail has its own tint and a thin divider. The service card and standard buttons use a one-DIP border. Other main content panels rely on surface contrast without outlines. There are no custom drop shadows, gradients, blur effects, or lifted hover surfaces in the implemented theme.

**The Quiet Surface Rule.** Use the existing surface, spacing, and border hierarchy for depth; preserve the flat visual character of these native panels.

## Shapes

Containers use the `panel` radius; buttons, navigation items, and the address inset use the `control` radius; the mode badge uses the `badge` radius. These gentle curves support a compact desktop utility rather than oversized pill controls. Native text boxes, combo boxes, radio buttons, checkboxes, and scrollbars retain their Windows control templates where the application does not replace them.

Navigation icons are simple outline paths (18 by 18 DIP), with rounded stroke caps and stroke thickness (1.6 DIP). The service indicator is a small circle (9 DIP), paired with text. These shapes complement labels and do not replace them.

## Components

### Buttons

The primary service button is blue with white semibold text. Its darker hover and pressed states are defined in the frontmatter; disabled opacity is 0.5. Standard buttons have white backgrounds, quiet borders, a pale blue hover fill, blue hover text, and the pressed-mist fill. Their disabled opacity is 0.45. The standard minimum height is 36 DIP; compact copy/export/folder actions use smaller explicit heights and padding.

Keyboard focus for themed buttons and navigation uses an inset dashed blue rectangle, stroke thickness (2 DIP), inset (3 DIP), and corner radius (3 DIP). Preserve this visible keyboard cue. State changes are immediate; the theme defines no animated button transitions.

### Inputs / Fields

Search uses a native text box with an explicit line-colored border, secondary font size, and the padding in `input`. An overlaid hint is visible only when the search field is empty. The log-level filter remains a native combo box; connection modes and startup preference remain native radio buttons and a checkbox. Retain their native keyboard and focus behavior rather than replacing them with decorative imitations.

### Navigation

Four vertically stacked radio-button items select Overview, Running Logs, Connection Settings, and Help. Each item pairs an outline icon with a Chinese label. Selected items use pale blue fill, blue text, and semibold weight; hover uses rail-hover. Items have outer margins (10 DIP horizontally, 2 DIP vertically). The rail includes the version and a short tray-behavior explanation at the bottom.

### Mode Badge

The compact pale blue badge identifies transparent or explicit proxy mode alongside the service state. It is informational and has no hover or action behavior.

### Cards / Containers

White, gently rounded surfaces contain service controls, traffic, logs, settings, and help. Keep related heading, explanation, and controls together. The shared service card provides the state dot, state text, explanation, mode badge, and one start/stop action on every page.

### Service Lifecycle

Starting, stopping, and exiting have explicit pending labels. Readiness follows a successful local status response; a running process alone is insufficient. The action is unavailable during an active start/stop operation. Connection mode controls are disabled while the core runs; the copy states that a changed mode applies at the next start. The startup preference means starting the service when the interface opens.

### Traffic

Two numeric columns separate download from upload, each with a cumulative total. The chart plots recent rate samples, retaining up to 60 points. The download series is solid blue with a very light fill; upload is a dashed slate line. Lines are two DIP thick, with no point markers, chart animation, or interactive tooltip. The chart begins with a centered, explanatory empty state and em dashes for unavailable values. Previously collected chart data is labeled as data from the previous run when unavailable.

### Logs

Search and a level filter sit above log actions and a vertically scrolling, virtualized list. Entries expose level, timestamp, source, and wrapping message text. A selected row uses pale blue; copying a selected item is disabled until a selection exists. Pause-auto-scroll, export, and clear actions remain visible. Empty collection and no-filter-match states have distinct explanations. A small footer reports visible records and the retention limit (500 entries).

### Help

Help is native, offline, scrollable text with short troubleshooting headings and spaced paragraphs. The external issue-discussion link is an explicit action. Its legacy control name does not imply embedded web content.

## Do's and Don'ts

### Do:

- **Do** preserve native Windows chrome, text rendering, and standard input behavior.
- **Do** keep service state, its explanation, and the start/stop action visible across pages.
- **Do** use the existing blue accent and cool surfaces from `Theme.xaml`.
- **Do** distinguish unavailable measurements, empty collections, and filtered-out results with explanatory text.
- **Do** check new content at the minimum window size and retain vertical scrolling where implemented.
- **Do** preserve visible keyboard focus and readable text labels alongside icons and colors.

### Don't:

- **Don't** equate local service readiness with remote GitHub reachability.
- **Don't** fill empty charts or logs with invented live data.
- **Don't** turn the desktop navigation into a web or mobile shell.
- **Don't** add custom shadows or animated hover effects to the existing flat theme.
- **Don't** describe the interface-open startup preference as Windows sign-in startup.
- **Don't** treat the sidecar's HTML/CSS previews as implementation templates for WPF.
