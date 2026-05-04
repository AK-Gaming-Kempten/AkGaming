import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import '../../AkGaming.Core/Theme/wwwroot/theme/akgaming-base-theme.css'
import './App.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
