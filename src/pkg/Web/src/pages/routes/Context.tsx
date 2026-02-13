import { createContext, useContext } from "react";
import IRoute from "./Route";

export interface IRoutesState {
  routes: IRoute[];
  activeRoute: IRoute | undefined;
}

export const initialState: IRoutesState = {
  routes: [],
  activeRoute: undefined,
};

export interface IRoutesContext {
  state: IRoutesState;
  dispatch: React.Dispatch<IAction>;
}

export type IAction = {
  type: string;
  payload: IRoute | IRoute[] | undefined;
};

export const RoutesContext = createContext<IRoutesContext>({
  state: initialState,
  dispatch: () => {},
});

// Actions
export const ADD_ROUTE = "ADD_ROUTE";
export const UPDATE_ROUTE = "UPDATE_ROUTE";
export const DELETE_ROUTE = "DELETE_ROUTE";
export const SET_ROUTES = "SET_ROUTES";
export const SET_ACTIVE_ROUTE = "SET_ACTIVE_ROUTE";

// Reducer
export const routesReducer = (state: IRoutesState, action: IAction) => {
  switch (action.type) {
    case ADD_ROUTE: {
      const addRoute = action.payload as IRoute;
      return {
        ...state,
        routes: [...state.routes, addRoute],
      };
    }
    case UPDATE_ROUTE: {
      const updateRoute = action.payload as IRoute;
      return {
        ...state,
        routes: state.routes.map((route) =>
          route.id === updateRoute.id ? updateRoute : route,
        ),
      };
    }
    case DELETE_ROUTE: {
      const deleteRoute = action.payload as IRoute;
      return {
        ...state,
        routes: state.routes.filter((route) => route.id !== deleteRoute.id),
      };
    }
    case SET_ROUTES: {
      const routes = action.payload as IRoute[];
      return {
        ...state,
        routes,
      };
    }
    case SET_ACTIVE_ROUTE: {
      const activeRoute = action.payload as IRoute;
      return {
        ...state,
        activeRoute,
      };
    }
    default:
      throw new Error();
  }
};

// Custom hook
function useRoutesContext() {
  return useContext(RoutesContext);
}

export { useRoutesContext };
