import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { NotificacoesProvider } from './context/NotificacoesContext';
import RotaProtegida from './components/RotaProtegida';
import LoginPage from './pages/LoginPage';
import CadastroPage from './pages/CadastroPage';
import HomePage from './pages/HomePage';
import PetsPage from './pages/PetsPage';
import PasseadoresPage from './pages/PasseadoresPage';
import SolicitarPasseioPage from './pages/SolicitarPasseioPage';
import PasseiosPage from './pages/PasseiosPage';
import NotificacoesPage from './pages/NotificacoesPage';
import PetFormPage from './pages/PetFormPage';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <NotificacoesProvider>
          <Routes>
            <Route path="/" element={<LoginPage />} />
            <Route path="/cadastro" element={<CadastroPage />} />
            <Route
              path="/home"
              element={
                <RotaProtegida>
                  <HomePage />
                </RotaProtegida>
              }
            />
            <Route
              path="/pets"
              element={
                <RotaProtegida>
                  <PetsPage />
                </RotaProtegida>
              }
            />
            <Route
              path="/pets/novo"
              element={
                <RotaProtegida>
                  <PetFormPage />
                </RotaProtegida>
              }
            />
            <Route
              path="/pets/:id/editar"
              element={
                <RotaProtegida>
                  <PetFormPage />
                </RotaProtegida>
              }
            />
            <Route
              path="/passeadores"
              element={
                <RotaProtegida>
                  <PasseadoresPage />
                </RotaProtegida>
              }
            />
            <Route
              path="/passeios/novo"
              element={
                <RotaProtegida>
                  <SolicitarPasseioPage />
                </RotaProtegida>
              }
            />
            <Route
              path="/passeios"
              element={
                <RotaProtegida>
                  <PasseiosPage />
                </RotaProtegida>
              }
            />
            <Route
              path="/notificacoes"
              element={
                <RotaProtegida>
                  <NotificacoesPage />
                </RotaProtegida>
              }
            />
          </Routes>
        </NotificacoesProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
