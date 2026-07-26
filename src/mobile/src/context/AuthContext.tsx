import React, { createContext, useContext } from 'react';

interface AuthContextValue {
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue>({
  logout: async () => {},
});

export function useAuth(): AuthContextValue {
  return useContext(AuthContext);
}
