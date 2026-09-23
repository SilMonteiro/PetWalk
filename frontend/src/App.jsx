import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import LoginScreen from './components/LoginScreen';
import PetRegistrationScreen from './components/PetRegistrationScreen';
import './styles/login.css';
import './styles/pet-registration.css';

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<LoginScreen />} />
        <Route path="/registro-pet" element={<PetRegistrationScreen />} />
      </Routes>
    </Router>
  );
}

export default App;