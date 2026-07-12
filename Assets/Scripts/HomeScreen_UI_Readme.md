Aguinaldo Shrine AR Tour — Home Screen UI (runtime builder)

What these files do
- HomeScreenUIController.cs
  * Builds the home screen UI at runtime (Canvas, background, header, feature cards, buttons, footer)
  * Exposes sprite/font references you must assign in the inspector
  * Provides a simple fade-in and start/exit handlers

- CardButton.cs
  * Lightweight pointer animations for card press/hover

- StartButtonPulse.cs
  * Pulsing glow animation for the Start button backdrop

How to use
1. Open your Unity project and locate Assets/Scripts.
2. Drag these scripts into your project (they're already in Assets/Scripts).
3. In the Hierarchy create an empty GameObject (e.g., "HomeScreenUI") and attach HomeScreenUIController.
4. Assign references in the inspector:
   - backgroundGradientSprite: (optional) vertical gradient sprite (navy->black)
   - shrineBlurSprite: (optional) blurred shrine image (for faint background overlay)
   - glowSprite: radial soft sprite (additive-looking radial)
   - iconArrow, iconCamera, iconMap: icons for the feature cards
   - tmpFont: a TextMeshPro font asset (recommended: Inter / Roboto / SF Pro)
   - arSceneName: (optional) name of AR scene to load when "Start Tour" is tapped
5. Play the scene — the UI will be created and animate in.

Styling notes
- Primary color: #0A1F44
- Accent glow: #3B82F6
- Card fill: white alpha ~0.06
- Card border: optional subtle stroke (1px, rgba(255,255,255,0.08))
- Font sizes (reference 1080w): label 14, title 36, subtitle 16, card title 18, card desc 14, start button 20

Animations
- Fade-in on load: canvas fades 0->1 in 0.6s
- Card hover/press: scale to 1.04 and press 0.96
- Start button pulse: glow scales 1.00->1.06 ping-pong

Recommended polish steps (manual)
- Create a 9-sliced rounded card sprite and assign as card Image sprite for perfect corners.
- Add a UI blur shader (URP + Shader Graph) or use a pre-blurred shrine texture for glassmorphism.
- Replace start button Image with a 2-color gradient sprite (left: #2051A8, right: #3B82F6) and use an additive radial glow for the backdrop.
- Consider using DOTween for richer animation control; scripts use coroutines as a compatibility-first fallback.

Performance
- Use pre-blurred textures rather than realtime UI blur for mobile performance.
- Reduce particle counts or disable the particle layer on low-end devices.

If you want, I can now:
- Add a simple Animator controller and animation clips instead of coroutines,
- Generate Unity-friendly placeholder PNGs (gradient, radial glow, icons) for quick visual testing,
- Or produce DOTween-based animation scripts for smoother curves.

Tell me which next step you prefer (or say "surprise me" and I'll pick the best polish).