import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import LoginScreen from './components/LoginScreen';
import HomeScreen from './components/HomeScreen';
import PetRegistrationScreen from './components/PetRegistrationScreen';
import RegisterScreen from './components/RegisterScreen';
import './styles/login.css';
import './styles/home.css';
import './styles/pet-registration.css';

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<LoginScreen />} />
        <Route path="/home" element={<HomeScreen />} />
        <Route path="/registro-pet" element={<PetRegistrationScreen />} />
        <Route path="/cadastro" element={<RegisterScreen />} />
      </Routes>
    </Router>
  );
}

export default App;