// import { useAuth0 } from "@auth0/auth0-react";
// import { Avatar, Button, Dropdown } from "flowbite-react";
// import { Link, useNavigate } from "react-router-dom";
// const isDevMode = import.meta.env.DEV;

// function Header() {
//   const {
//     isLoading,
//     isAuthenticated,
//     user,
//     loginWithRedirect,
//     logout,
//     getAccessTokenSilently,
//   } = useAuth0();
//   const navigate = useNavigate();

//   const handleCopyAccessTokenClick = async () => {
//     const accessToken = await getAccessTokenSilently();
//     navigator.clipboard.writeText(accessToken);
//   };

//   if (isLoading) {
//     return null;
//   }

//   return (
//     <nav className="flex items-center border-b-[1px] border-b-gray-300 px-8 py-2">
//       <Link to="/" className="">
//         <span className="self-center whitespace-nowrap text-xl font-semibold">
//           MycroCloud
//         </span>
//       </Link>
//       {isAuthenticated ? (
//         <>
//           <ul className="me-auto ms-4 flex space-x-2">
//             <li>
//               <Link
//                 className="block border-b border-gray-100 py-2 pl-3 pr-4 text-gray-700 hover:bg-gray-50 md:border-0 md:p-0 md:hover:bg-transparent md:hover:text-cyan-700"
//                 to="apps"
//               >
//                 Apps
//               </Link>
//             </li>
//           </ul>
//           <a
//             className="me-2 ms-auto block border-b border-gray-100 py-2 pl-3 pr-4 text-gray-700 hover:bg-gray-50 md:border-0 md:p-0 md:hover:bg-transparent md:hover:text-cyan-700"
//             href="https://docs.mycrocloud.info" // todo: use env variable
//             rel="noopener noreferrer"
//             target="_blank"
//           >
//             Docs
//           </a>
//           <Dropdown
//             arrowIcon={false}
//             inline
//             label={<Avatar alt="User settings" img={user?.picture} rounded />}
//           >
//             <Dropdown.Header>
//               <span className="block">{user?.name}</span>
//               <span className="block truncate font-medium">{user?.email}</span>
//             </Dropdown.Header>
//             <Dropdown.Item onClick={() => navigate("/settings")}>
//               Account Settings
//             </Dropdown.Item>
//             <Dropdown.Divider />
//             <Dropdown.Item onClick={() => logout()}>Log out</Dropdown.Item>
//             {isDevMode && (
//               <Dropdown.Item onClick={handleCopyAccessTokenClick}>
//                 Copy access token
//               </Dropdown.Item>
//             )}
//           </Dropdown>
//         </>
//       ) : (
//         <Button
//           className="ms-auto"
//           size="sm"
//           onClick={() => loginWithRedirect()}
//         >
//           Log in
//         </Button>
//       )}
//     </nav>
//   );
// }
// export default Header;




import {
  Avatar,
  Dropdown,
  DropdownDivider,
  DropdownHeader,
  DropdownItem,
  Navbar,
  NavbarBrand,
  NavbarCollapse,
  NavbarLink,
  NavbarToggle,
} from "flowbite-react";
import { Link } from "react-router-dom";

export default function Header() {
  return (
    <Navbar fluid rounded>
      <NavbarBrand as={Link} href="/">
        <img src="/cloud.svg" className="mr-3 h-6 sm:h-9" alt="MycroCloud Logo" />
        <span className="self-center whitespace-nowrap text-xl font-semibold dark:text-white">MycroCloud</span>
      </NavbarBrand>
      <div className="flex md:order-2">
        <Dropdown
          arrowIcon={false}
          inline
          label={
            <Avatar alt="User settings" img="https://flowbite.com/docs/images/people/profile-picture-5.jpg" rounded />
          }
        >
          <DropdownHeader>
            <span className="block text-sm">Bonnie Green</span>
            <span className="block truncate text-sm font-medium">name@flowbite.com</span>
          </DropdownHeader>
          <DropdownItem>Dashboard</DropdownItem>
          <DropdownItem>Settings</DropdownItem>
          <DropdownItem>Earnings</DropdownItem>
          <DropdownDivider />
          <DropdownItem>Sign out</DropdownItem>
        </Dropdown>
        <NavbarToggle />
      </div>
      <NavbarCollapse>
        <NavbarLink href="#" active>
          Home
        </NavbarLink>
        <NavbarLink href="#">About</NavbarLink>
        <NavbarLink href="#">Services</NavbarLink>
        <NavbarLink href="#">Pricing</NavbarLink>
        <NavbarLink href="#">Contact</NavbarLink>
      </NavbarCollapse>
    </Navbar>
  );
}
